using Kwerty.DviZe.Resilience;
using Kwerty.DviZe.Win;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidHandle(HidDevice device, HidHandleOptions options, RetryPolicy retryPolicy, ILoggerFactory loggerFactory) : IHidHandle
{
    readonly internal HidDevice device = device;
    readonly IDelayGenerator retryDelayGenerator = retryPolicy;
    readonly ILogger logger = loggerFactory.CreateLogger<HidHandle>();
    SafeFileHandle fileHandle;
    internal bool hasAttachedStream;
    internal bool closed;

    internal HidAccessMode AccessMode => options.AccessMode;

    internal async Task InitAsync(CancellationToken cancellationToken)
    {
        try
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    fileHandle = Win32.CreateFile(device.devicePath, GetWinAccessMode(), GetWinShareMode(), IntPtr.Zero, Win32.OPEN_EXISTING, Win32.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                    if (fileHandle.IsInvalid)
                    {
                        throw Win32Exception.FromLastError(nameof(Win32.CreateFile));
                    }

                    break;
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == Win32.ERROR_SHARING_VIOLATION)
                {
                    if (!retryDelayGenerator.TryNext(++attempt, out var delay))
                    {
                        throw;
                    }

                    logger.LogDebug("{devicePath} sharing violation. Retrying in {delay}ms.", device.devicePath, delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Win32Exception ex)
        {
            throw ex.NativeErrorCode == Win32.ERROR_ACCESS_DENIED ? new HidAccessException(innerException: ex)
                : ex.NativeErrorCode == Win32.ERROR_SHARING_VIOLATION ? new HidAccessConflictException(innerException: ex)
                : new HidException(innerException: ex);
        }
    }

    public void Dispose()
    {
        if (!Interlocked.Exchange(ref closed, true))
        {
            fileHandle?.Dispose();
        }
    }

    uint GetWinAccessMode()
    {
        return options.AccessMode switch
        {
            HidAccessMode.None => 0,
            HidAccessMode.ReadWrite => Win32.GENERIC_READ | Win32.GENERIC_WRITE,
            HidAccessMode.Read => Win32.GENERIC_READ,
            HidAccessMode.Write => Win32.GENERIC_WRITE,
            _ => throw new NotImplementedException()
        };
    }

    uint GetWinShareMode()
    {
        uint shareMode = 0;
        if (!options.ExclusiveRead)
        {
            shareMode |= Win32.FILE_SHARE_READ;
        }
        if (!options.ExclusiveWrite)
        {
            shareMode |= Win32.FILE_SHARE_WRITE;
        }
        return shareMode;
    }

    internal FileAccess GetFileStreamAccessMode()
    {
        return options.AccessMode switch
        {
            HidAccessMode.ReadWrite => FileAccess.ReadWrite,
            HidAccessMode.Read => FileAccess.Read,
            HidAccessMode.Write => FileAccess.Write,
            _ => throw new NotImplementedException()
        };
    }

    internal SafeFileHandle GetFileHandle(bool ownsHandle)
    {
        return ownsHandle
            ? fileHandle
            : new SafeFileHandle(fileHandle.DangerousGetHandle(), ownsHandle: false);
    }
}
