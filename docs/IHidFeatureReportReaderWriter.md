# IHidFeatureReportReaderWriter

**Namespace:** `Kwerty.DviZe.Hid`

Reads and writes HID feature reports.

## Read

```csharp
public void Read(Span<byte> buffer);
```

Reads a feature report into `buffer`.

The first byte contains the desired report ID.

Throws `InvalidOperationException` if the buffer size does not match [IHidDevice](IHidDevice.md).[FeatureReportSize](IHidDevice.md#Properties).

Throws `HidException` if the device has dismounted or the handle is closed.

## Write

```csharp
public void Write(ReadOnlySpan<byte> buffer);
```

Writes a feature report from `buffer`.

The first byte contains the report ID.

Throws `InvalidOperationException` if the buffer size does not match [IHidDevice](IHidDevice.md).[FeatureReportSize](IHidDevice.md#Properties).

Throws `HidException` if the device has dismounted or the handle is closed.

## Dispose

```csharp
public void Dispose();
```

Prevents future reads/writes and closes the handle.
