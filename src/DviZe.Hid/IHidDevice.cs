using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid;

public interface IHidDevice
{
    int VendorId { get; }

    int ProductId { get; }

    int? InterfaceNumber { get; }

    string VendorName { get; }

    string ProductName { get; }

    string SerialNumber { get; }

    /// <summary>
    /// The device release number, also known as device version number.
    /// </summary>
    /// <remarks>
    /// This value is typically expressed in Binary Coded Decimal (BCD) format (eg.. 0x0210 for v2.1.0).
    /// </remarks>
    int ReleaseNumber { get; }

    /// <summary>
    /// A platform-specific identifier used to uniquely identify the device.
    /// </summary>
    /// <remarks>
    /// Note: This value is not guaranteed to persist across sessions on all operating systems (macOS in particular).
    /// </remarks>
    string PlatformDeviceId { get; }

    /// <summary>
    /// A platform-specific identifier used to group multiple HID interfaces that belong to the same physical device.
    /// <remarks>
    /// Note: This value is not guaranteed to persist across sessions on all operating systems (macOS in particular).
    /// </remarks>
    string PlatformContainerId { get; }

    int Usage { get; }

    int UsagePage { get; }

    int InputReportSize { get; }

    int OutputReportSize { get; }

    int FeatureReportSize { get; }

    Task Dismounted { get; }

    Task<IHidHandle> GetHandleAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);

    Task<IHidHandle> GetHandleAsync(HidHandleOptions options, CancellationToken cancellationToken = default);

    Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);

    Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidHandleOptions options, CancellationToken cancellationToken = default);

    Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(IHidHandle handle, CancellationToken cancellationToken = default);

    Task<Stream> GetStreamAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);

    Task<Stream> GetStreamAsync(HidHandleOptions options, CancellationToken cancellationToken = default);

    Task<Stream> GetStreamAsync(IHidHandle handle, CancellationToken cancellationToken = default);
}