using Kwerty.DviZe.Resilience;
using Kwerty.DviZe.Win;
using Kwerty.DviZe.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidEnumeratorSession(HidOptions options, ILoggerFactory loggerFactory) : Worker
{
    readonly TaskScheduler taskScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
    readonly Dictionary<string, HidDevice> deviceWorkers = [];
    readonly List<HidDevice> devices = [];
    readonly List<HidSubscription> subscriptions = [];
    readonly Runner<HidDevice> deviceRunner = new(loggerFactory);
    readonly ILogger logger = loggerFactory.CreateLogger<HidEnumeratorSession>();
	Guid hidGuid;
	Win32.SafeCmNotificationHandle notificationRegistration;
	Win32.CM_NOTIFY_CALLBACK notificationHandler;
	Task notificationProcessingTask;

    internal CancellationToken StoppingToken => Context.StoppingToken; // Used by subscriptions.

    protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
	{
        if (hidGuid == Guid.Empty)
        {
            Win32.HidD_GetHidGuid(out hidGuid);
        }

        var filter = new Win32.CM_NOTIFY_FILTER__DeviceInterface
        {
            cbSize = Marshal.SizeOf<Win32.CM_NOTIFY_FILTER__DeviceInterface>(),
            FilterType = Win32.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE,
            ClassGuid = hidGuid,
        };

        var notifications = Channel.CreateUnbounded<Notification>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        notificationHandler = (_, _, action, ref eventData, eventDataSize) =>
        {
            if (action == Win32.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL
                || action == Win32.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL)
            {
                var eventDataSpan = MemoryMarshal.CreateSpan(ref eventData, eventDataSize);
                var pathOffset = Marshal.SizeOf<Win32.CM_NOTIFY_EVENT_DATA__DeviceInterfaceHeader>();
                var pathChars = MemoryMarshal.Cast<byte, char>(eventDataSpan[pathOffset..]);

                notifications.Writer.TryWrite(new Notification
                {
                    devicePath = new string(pathChars[..^1]), // Null terminated.
                    evt = action == Win32.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL ? HidEventType.DeviceMounted : HidEventType.DeviceDismounted,
                });
            }
            return Win32.ERROR_SUCCESS;
        };

        var registerResult = Win32.CM_Register_Notification(ref filter, IntPtr.Zero, notificationHandler, out notificationRegistration);
        if (registerResult != Win32.CR_SUCCESS)
        {
            throw Win32Exception.FromError(nameof(Win32.CM_Register_Notification), registerResult);
        }

        char[] listBuffer;
        {
            IDelayGenerator retryDelayGenerator = new RetryPolicy(TimeSpan.Zero, maxRetries: 3);

            var attempt = 0;
            while (true)
            {
                var sizeResult = Win32.CM_Get_Device_Interface_List_Size(out var listSize, hidGuid, IntPtr.Zero, Win32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
                if (sizeResult != Win32.CR_SUCCESS)
                {
                    throw Win32Exception.FromError(nameof(Win32.CM_Get_Device_Interface_List_Size), sizeResult);
                }

                listBuffer = new char[listSize];
                var listResult = Win32.CM_Get_Device_Interface_List(hidGuid, IntPtr.Zero, listBuffer, listSize, Win32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
                if (listResult == Win32.CR_SUCCESS)
                {
                    break;
                }
                else if (listResult == Win32.CR_BUFFER_SMALL)
                {
                    // The list has grown.
                    if (retryDelayGenerator.TryNext(++attempt, out _))
                    {
                        continue;
                    }
                }
                throw Win32Exception.FromError(nameof(Win32.CM_Get_Device_Interface_List), listResult);
            }
        }

        var initialNotifications = Channel.CreateUnbounded<Notification>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        for (var pos = 0; pos < listBuffer.Length;)
        {
            if (listBuffer[pos] == '\0')
            {
                break;
            }

            var len = Array.IndexOf(listBuffer, '\0', pos) - pos;

            initialNotifications.Writer.TryWrite(new Notification
            {
                devicePath = new string(listBuffer, pos, len),
                evt = HidEventType.DeviceMounted,
            });

            pos += len + 1;
        }
        initialNotifications.Writer.Complete();

        await ProcessNotificationsAsync(initialNotifications.Reader, CancellationToken.None).ConfigureAwait(false);

        notificationProcessingTask = ProcessNotificationsAsync(notifications.Reader, Context.StoppingToken);
    }

	protected override async Task OnStoppingAsync()
	{
		await notificationProcessingTask.ConfigureAwait(false);
		await deviceRunner.DisposeAsync().ConfigureAwait(false);
        notificationRegistration.Dispose();
    }

	async Task ProcessNotificationsAsync(ChannelReader<Notification> notifications, CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var notification in notifications.ReadAllAsync(cancellationToken).ConfigureAwait(false))
			{
                if (notification.evt == HidEventType.DeviceMounted)
                {
                    var deviceWorker = new HidDevice(this, options, notification.devicePath, loggerFactory);
                    if (deviceWorkers.TryAdd(notification.devicePath, deviceWorker))
                    {
                        await deviceRunner.StartWorkerAsync(deviceWorker, CancellationToken.None).ConfigureAwait(false); // Runs synchronously.
                    }
                }
                else if (notification.evt == HidEventType.DeviceDismounted)
                {
                    if (deviceWorkers.Remove(notification.devicePath, out var deviceWorker))
                    {
                        deviceWorker.TryStop();
                    }
                }
            }
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
            return;
		}
	}

    internal void AddDevice(HidDevice device)
    {
        lock (devices)
        {
            devices.Add(device);

            var evt = new HidEvent(device, HidEventType.DeviceMounted);
            foreach (var subscription in subscriptions)
            {
                if (subscription.IsEligibleForEvent(HidEventType.DeviceMounted))
                {
                    _ = Task.CompletedTask.ContinueWith(_ => subscription.callback.Invoke(evt),
                        CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, taskScheduler);
                }
            }
        }
    }

    internal void RemoveDevice(HidDevice device)
    {
        lock (devices)
        {
            devices.Remove(device);

            var evt = new HidEvent(device, HidEventType.DeviceDismounted);
            foreach (var subscription in subscriptions)
            {
                if (subscription.IsEligibleForEvent(HidEventType.DeviceDismounted))
                {
                    _ = Task.CompletedTask.ContinueWith(_ => subscription.callback.Invoke(evt),
                        CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, taskScheduler);
                }
            }
        }
    }

    internal void AddSubscription(HidSubscription subscription)
    {
        lock (devices)
        {
            if (subscription.IsEligibleForEvent(HidEventType.DeviceMounted))
            {
                foreach (var device in devices)
                {
                    var evt = new HidEvent(device, HidEventType.DeviceMounted);
                    _ = Task.CompletedTask.ContinueWith(_ => subscription.callback.Invoke(evt),
                        CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, taskScheduler);
                }
            }

            subscriptions.Add(subscription);
        }
    }

    internal void RemoveSubscription(HidSubscription subscription)
    {
        lock (devices)
        {
            subscriptions.Remove(subscription);
        }
    }

    class Notification
    {
        public string devicePath;
        public HidEventType evt;
    }
}
