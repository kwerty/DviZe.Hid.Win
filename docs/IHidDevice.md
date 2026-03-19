# IHidDevice

**Namespace:** `Kwerty.DviZe.Hid`

Represents a mounted HID device.

## Properties

| Property                  | Type      | Description
| :--                       | :--       | :--
| `VendorId`                | `int`     | Vendor ID.
| `ProductId`               | `int`     | Product ID.
| `InterfaceNumber`         | `int?`    | Interface number.
| `VendorName`              | `string`  | Vendor name.
| `ProductName`             | `string`  | Product name.
| `SerialNumber`            | `string`  | Serial number or `null`.
| `ReleaseNumber`           | `int`     | Device version (typically expressed in BCD format, eg.. `0x0210` for `v2.1.0`).
| `PlatformDeviceId`        | `string`  | Uniquely identifies the device.
| `PlatformContainerId`     | `string`  | Used to group multiple HID interfaces that belong to the same physical device.
| `Usage`                   | `int`     | HID usage.
| `UsagePage`               | `int`     | HID usage page.
| `InputReportSize`         | `int`     | Size of input reports in bytes.
| `OutputReportSize`        | `int`     | Size of output reports in bytes.
| `FeatureReportSize`       | `int`     | Size of feature reports in bytes.
| `Dismounted`              | `Task`    | A task that completes when the device is dismounted.

## GetHandleAsync

```csharp
public Task<IHidHandle> GetHandleAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);
public Task<IHidHandle> GetHandleAsync(HidHandleOptions options, CancellationToken cancellationToken = default);
```

Creates a handle (`IHidHandle`) for the device.

Throws `HidException` if the device has dismounted, access was denied (`HidAccessException`), or there was a conflict with another handle (`HidAccessConflictException`).

`IHandle` must be disposed to close the handle.

### HidAccessMode

| Value         | Description
| :--           | :--
| `None`        | No access.
| `Read`        | Read access (input reports).
| `Write`       | Write access (output reports).
| `ReadWrite`   | Read and write access.

### HidHandleOptions

| Property          | Type              | Default               | Description
| :--               | :--               | :--                   | :--
| `AccessMode`      | `HidAccessMode`   | `HidAccessMode.None`  | The access mode.
| `ExclusiveRead`   | `bool`            | `false`               | Exclusive read access.
| `ExclusiveWrite`  | `bool`            | `false`               | Exclusive write access.

## GetFeatureReportReaderWriterAsync

```csharp
public Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);
public Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(HidHandleOptions options, CancellationToken cancellationToken = default);
public Task<IHidFeatureReportReaderWriter> GetFeatureReportReaderWriterAsync(IHidHandle handle, CancellationToken cancellationToken = default);
```

Creates an [IHidFeatureReportReaderWriter](IHidFeatureReportReaderWriter.md) for reading and/or writing feature reports.

If a handle is supplied, the caller retains handle ownership. Otherwise a new handle is created, and `IHidFeatureReportReaderWriter` will own it.

Throws `HidException` if the device has dismounted or the handle is closed.

In cases where a handle must be created, `HidAccessException` or `HidAccessConflictException` may also be thrown.

`IHidFeatureReportReaderWriter` must be disposed regardless of handle ownership.

## GetStreamAsync

```csharp
public Task<Stream> GetStreamAsync(HidAccessMode accessMode, CancellationToken cancellationToken = default);
public Task<Stream> GetStreamAsync(HidHandleOptions options, CancellationToken cancellationToken = default);
public Task<Stream> GetStreamAsync(IHidHandle handle, CancellationToken cancellationToken = default);
```

Creates a `Stream` (`System.IO`) for reading input reports and/or writing output reports.

If a handle is supplied, the caller retains handle ownership. Otherwise a new handle is created, and `Stream` will own it.

Throws `HidException` if the device has dismounted or the handle is closed.

In cases where a handle must be created, `HidAccessException` or `HidAccessConflictException` may also be thrown.

`Stream` must be disposed regardless of handle ownership.
