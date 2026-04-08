using Kwerty.DviZe.Win;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidFeatureReportReaderWriter(SafeFileHandle fileHandle, int featureReportSize) : IHidFeatureReportReaderWriter
{
	volatile bool disposed;

    public void Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (buffer.Length != featureReportSize)
        {
            throw new InvalidOperationException();
        }

        if (!Win32.HidD_GetFeature(fileHandle, ref MemoryMarshal.GetReference(buffer), buffer.Length))
        {
            var nativeException = Win32Exception.FromLastError(nameof(Win32.HidD_GetFeature));
            throw new HidException(innerException: nativeException);
        }
    }

    public void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (buffer.Length != featureReportSize)
        {
            throw new InvalidOperationException();
        }

        if (!Win32.HidD_SetFeature(fileHandle, ref MemoryMarshal.GetReference(buffer), buffer.Length))
        {
            var nativeException = Win32Exception.FromLastError(nameof(Win32.HidD_SetFeature));
            throw new HidException(innerException: nativeException);
        }
    }

    public void Dispose()
	{
        if (!Interlocked.Exchange(ref disposed, true))
        {
            fileHandle.Dispose();
        }
    }
}