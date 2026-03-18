using Kwerty.DviZe.Resilience;
using Kwerty.DviZe.Win;
using Kwerty.DviZe.Workers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidDevice(HidEnumeratorSession session, HidOptions options, string devicePath, ILoggerFactory loggerFactory)
    : Worker, IHidDevice
{
    readonly internal HidOptions options = options;
    readonly internal string devicePath = devicePath;
    readonly DevicePathInfo devicePathInfo = new(devicePath);
    readonly ILogger logger = loggerFactory.CreateLogger<HidDevice>();
    Task initTask;
    string longInstanceId;
    string containerId;
    Win32.HIDD_ATTRIBUTES attributes;
    Win32.HIDP_CAPS capabilities;

    public int VendorId => devicePathInfo.VendorId;

    public int ProductId => devicePathInfo.ProductId;

    public int? InterfaceNumber => devicePathInfo.InterfaceNumber;

    public string VendorName { get; private set; }

    public string ProductName { get; private set; }

    public string SerialNumber { get; private set; }

    public int ReleaseNumber => attributes.VersionNumber;

    public string PlatformDeviceId => devicePathInfo.InstanceId;

    public string PlatformContainerId => containerId;

    public int Usage => capabilities.Usage;

    public int UsagePage => capabilities.UsagePage;

    public int InputReportSize => capabilities.InputReportByteLength;

    public int OutputReportSize => capabilities.OutputReportByteLength;

    public int FeatureReportSize => capabilities.FeatureReportByteLength;

    public Task Dismounted => Context.Stopped;

    protected override Task OnStartingAsync(WorkerStartingContext startingContext)
    {
        initTask = Task.Run(InitAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    async Task InitAsync()
    {
        try
        {
            longInstanceId = GetLongInstanceId(devicePath);
            containerId = GetContainerId(longInstanceId);

            using var handle = new HidHandle(this, new HidHandleOptions(HidAccessMode.None), options.MetaDataRetryPolicy, loggerFactory);
            await handle.InitAsync(Context.StoppingToken).ConfigureAwait(false);

            VendorName = GetVendorName(handle.GetFileHandle(ownsHandle: true));
            ProductName = GetProductName(handle.GetFileHandle(ownsHandle: true));
            SerialNumber = GetSerialNumber(handle.GetFileHandle(ownsHandle: true));
            attributes = GetAttributes(handle.GetFileHandle(ownsHandle: true));
            capabilities = GetCapabilities(handle.GetFileHandle(ownsHandle: true));

            session.AddDevice(this);
        }
        catch (Exception ex) when (!Context.StoppingToken.IsCancellationRequested)
        {
            logger.LogDebug(ex, "{devicePath} initialisation failed.", devicePath);
            throw;
        }
    }

    protected override async Task OnStoppingAsync()
    {
        try
        {
            await initTask.ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        session.RemoveDevice(this);
    }

    public Task<IHidHandle> GetHandleAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default)
        => GetHandleAsync(new HidHandleOptions(accessMode), cancellationToken);

    public async Task<IHidHandle> GetHandleAsync(HidHandleOptions handleOptions, CancellationToken cancellationToken = default)
        => await GetHandleAsyncCore(handleOptions, cancellationToken).ConfigureAwait(false);

    async Task<HidHandle> GetHandleAsyncCore(HidHandleOptions handleOptions, CancellationToken cancellationToken)
    {
        if (Context.StoppingToken.IsCancellationRequested)
        {
            throw new HidException();
        }

        var handle = new HidHandle(this, handleOptions, RetryPolicy.None, loggerFactory);
        await handle.InitAsync(cancellationToken).ConfigureAwait(false);
        return handle;
    }

    public Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(IHidHandle handle, CancellationToken cancellationToken = default)
    {
        if (handle is not HidHandle winHandle
            || winHandle.device != this)
        {
            throw new InvalidOperationException();
        }

        if (winHandle.closed)
        {
            throw new HidException();
        }

        return GetFeatureReportReaderWriterAsyncCore(winHandle, ownsHandle: false);
    }

    public Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default)
        => GetFeatureReportReaderWriterAsync(new HidHandleOptions(accessMode), cancellationToken);

    public async Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidHandleOptions options, CancellationToken cancellationToken = default)
    {
        var handle = await GetHandleAsyncCore(options, cancellationToken).ConfigureAwait(false);
        return await GetFeatureReportReaderWriterAsyncCore(handle, ownsHandle: true).ConfigureAwait(false);
    }

    Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsyncCore(HidHandle handle, bool ownsHandle)
    {
        var featureReportReaderWriter = new HidFeatureReportReaderWriter(handle.GetFileHandle(ownsHandle), FeatureReportSize);
        return Task.FromResult<IHidFeatureReportReaderWriter>(featureReportReaderWriter);
    }

    public Task<Stream> GetStreamAsync(IHidHandle handle, CancellationToken cancellationToken = default)
    {
        if (handle is not HidHandle winHandle
            || winHandle.device != this
            || winHandle.AccessMode == HidAccessMode.None
            || Interlocked.Exchange(ref winHandle.hasAttachedStream, true))
        {
            throw new InvalidOperationException();
        }

        if (winHandle.closed)
        {
            throw new HidException();
        }

        return GetStreamAsyncCore(winHandle, ownsHandle: false);
    }

    public Task<Stream> GetStreamAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default)
        => GetStreamAsync(new HidHandleOptions(accessMode), cancellationToken);

    public async Task<Stream> GetStreamAsync(HidHandleOptions options, CancellationToken cancellationToken = default)
    {
        var handle = await GetHandleAsyncCore(options, cancellationToken).ConfigureAwait(false);
        return await GetStreamAsyncCore(handle, ownsHandle: true).ConfigureAwait(false);
    }

    static Task<Stream> GetStreamAsyncCore(HidHandle handle, bool ownsHandle)
    {
        try
        {
            // To disable the FileStream buffering, just pass 1 (works for every .NET) or 0 (works for .NET 6 preview 6+) as bufferSize.
            // https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/
            var stream = new FileStream(handle.GetFileHandle(ownsHandle), handle.GetFileStreamAccessMode(), bufferSize: 0, isAsync: true);
            return Task.FromResult<Stream>(stream);
        }
        catch (IOException ex)
        {
            return Task.FromException<Stream>(new HidException(innerException: ex));
        }
    }

    internal bool TryStop() => Context.TryStop(); // Called by enumerator.

    static string GetLongInstanceId(string devicePath)
    {
        var propKey = Win32.DEVPKEY_Device_InstanceId;
        var bufferSize = 0;

        var sizeResult = Win32.CM_Get_Device_Interface_Property(devicePath, ref propKey, out _, IntPtr.Zero, ref bufferSize, 0);
        if (sizeResult != Win32.CR_BUFFER_SMALL)
        {
            throw Win32Exception.FromError(nameof(Win32.CM_Get_Device_Interface_Property), sizeResult);
        }

        var bufferPtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var propResult = Win32.CM_Get_Device_Interface_Property(devicePath, ref propKey, out _, bufferPtr, ref bufferSize, 0);
            if (propResult != Win32.CR_SUCCESS)
            {
                throw Win32Exception.FromError(nameof(Win32.CM_Get_Device_Interface_Property), propResult);
            }

            return Marshal.PtrToStringUni(bufferPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    static string GetContainerId(string longInstanceId)
    {
        int nodeResult = Win32.CM_Locate_DevNode(out var nodeHandle, longInstanceId, Win32.CM_LOCATE_DEVNODE_NORMAL);
        if (nodeResult != Win32.CR_SUCCESS)
        {
            throw Win32Exception.FromError(nameof(Win32.CM_Locate_DevNode), nodeResult);
        }

        var propKey = Win32.DEVPKEY_Device_ContainerId;
        var bufferSize = 0;

        var sizeResult = Win32.CM_Get_DevNode_Property(nodeHandle, ref propKey, out _, IntPtr.Zero, ref bufferSize, 0);
        if (sizeResult != Win32.CR_BUFFER_SMALL)
        {
            throw Win32Exception.FromError(nameof(Win32.CM_Get_DevNode_Property), sizeResult);
        }

        var bufferPtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var propResult = Win32.CM_Get_DevNode_Property(nodeHandle, ref propKey, out _, bufferPtr, ref bufferSize, 0);
            if (propResult != Win32.CR_SUCCESS)
            {
                throw Win32Exception.FromError(nameof(Win32.CM_Get_DevNode_Property), propResult);
            }

            return Marshal.PtrToStructure<Guid>(bufferPtr).ToString();
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }

    static string GetVendorName(SafeFileHandle fileHandle)
    {
        var buffer = new byte[HidConstants.StringDescriptorMaxBytes];
        if (!Win32.HidD_GetManufacturerString(fileHandle, buffer, buffer.Length))
        {
            throw Win32Exception.FromLastError(nameof(Win32.HidD_GetManufacturerString));
        }
        return GetStringFromNullTerminated(buffer);
    }

    static string GetProductName(SafeFileHandle fileHandle)
    {
        var buffer = new byte[HidConstants.StringDescriptorMaxBytes];
        if (!Win32.HidD_GetProductString(fileHandle, buffer, buffer.Length))
        {
            throw Win32Exception.FromLastError(nameof(Win32.HidD_GetProductString));
        }
        return GetStringFromNullTerminated(buffer);
    }

    static string GetSerialNumber(SafeFileHandle fileHandle)
    {
        try
        {
            var buffer = new byte[HidConstants.StringDescriptorMaxBytes];
            if (!Win32.HidD_GetSerialNumberString(fileHandle, buffer, buffer.Length))
            {
                throw Win32Exception.FromLastError(nameof(Win32.HidD_GetSerialNumberString));
            }
            return GetStringFromNullTerminated(buffer);
        }   
        catch (Win32Exception ex) when (ex.NativeErrorCode == Win32.ERROR_INVALID_PARAMETER)
        {
            return null;
        }
    }

    static Win32.HIDD_ATTRIBUTES GetAttributes(SafeFileHandle fileHandle)
    {
        var attribs = new Win32.HIDD_ATTRIBUTES
        {
            Size = Marshal.SizeOf<Win32.HIDD_ATTRIBUTES>(),
        };
        if (!Win32.HidD_GetAttributes(fileHandle, ref attribs))
        {
            throw Win32Exception.FromLastError(nameof(Win32.HidD_GetAttributes));
        }
        return attribs;
    }

    static Win32.HIDP_CAPS GetCapabilities(SafeFileHandle fileHandle)
    {
        if (!Win32.HidD_GetPreparsedData(fileHandle, out var dataHandle))
        {
            throw Win32Exception.FromLastError(nameof(Win32.HidD_GetPreparsedData));
        }

        using (dataHandle)
        {
            var result = Win32.HidP_GetCaps(dataHandle, out var capabilities);
            if (result != Win32.HIDP_STATUS_SUCCESS)
            {
                throw Win32Exception.FromLastError(nameof(Win32.HidP_GetCaps));
            }

            return capabilities;
        }
    }

    static string GetStringFromNullTerminated(Span<byte> bytes)
    {
        var asChars = MemoryMarshal.Cast<byte, char>(bytes);
        var len = asChars.IndexOf(char.MinValue);
        return new string(asChars[..len]);
    }
}