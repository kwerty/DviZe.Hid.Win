using System;

namespace Kwerty.DviZe.Hid;

public interface IHidFeatureReportReaderWriter : IDisposable
{
    /// <summary>
    /// Reads an HID feature report into <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// The first byte of <paramref name="buffer"/> must be set to the desired report ID before calling.
    /// The buffer will be populated with the report data, with the report ID retained in the first byte.
    /// The buffer length must match the feature report size.
    /// </remarks>
    /// <param name="buffer">The buffer to read the feature report into. The first byte must be set to the report ID.</param>
    void Read(Span<byte> buffer);

    /// <summary>
    /// Writes an HID feature report from <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// The first byte of <paramref name="buffer"/> must be set to the report ID.
    /// The buffer length must match the feature report size.
    /// </remarks>
    /// <param name="buffer">The buffer containing the feature report data to write. The first byte must be set to the report ID.</param>
    void Write(ReadOnlySpan<byte> buffer);
}
