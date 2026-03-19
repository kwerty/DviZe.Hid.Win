# DviZe.Hid.Win

A .NET 10 class library for enumerating and communicating with HID devices in Windows.

## Enumerating HID Devices

Create an [HidEnumerator](docs/HidEnumerator.md) and call [SubscribeAsync](docs/HidEnumerator.md#SubscribeAsync) with a callback to receive [HidEvent](docs/HidEnumerator.md#HidEvent) notifications for all current devices, as well as future events.

Each [HidEvent](docs/HidEnumerator.md#HidEvent) has an `EventType` ([HidEventType](docs/HidEnumerator.md#HidEvent)) and a `Device` ([IHidDevice](docs/IHidDevice.md)).

```csharp
using Kwerty.DviZe.Hid;
using Kwerty.DviZe.Hid.Win;

var hidEnumerator = new HidEnumerator(loggerFactory);

var hidSubscription = await hidEnumerator.SubscribeAsync(evt =>
{
    if (evt.EventType == HidEventType.DeviceMounted
        && evt.Device.VendorId == 0x046D)
    {
        Console.WriteLine($"{evt.Device.ProductName} mounted.");
    }
});
```

## Feature Reports

Consider a gaming mouse whose configuration can be read/written via a feature report.

The manufacturer has defined the feature report as follows:

| Byte(s)   | Field     | Type                      | Description
| :--       | :--       | :--                       | :--
| 0         | Report ID | `byte`                    | Always `0x03`.
| 1-2       | Mouse DPI | `uint16` (Little endian)  | DPI value between 1000-5000.
| 3         | Game Mode | `byte`                    | `1` = enabled, `0` = disabled.

To read and write the report, obtain an [IHidFeatureReportReaderWriter](docs/IHidFeatureReportReaderWriter.md) from [IHidDevice.GetFeatureReportReaderWriterAsync](docs/IHidDevice.md#GetFeatureReportReaderWriterAsync).

```csharp
using var featureRw = await device.GetFeatureReportReaderWriterAsync(HidAccessMode.None, cancellationToken);

// Read mouse configuration.

var buffer = new byte[device.FeatureReportSize];
buffer[0] = 0x03; // Report ID.

featureRw.Read(buffer);

var mouseDpi = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(1, 2));
var gameModeEnabled = Convert.ToBoolean(buffer[3]);

// Update mouse configuration.

mouseDpi = 1600;
gameModeEnabled = true;

BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(1, 2), mouseDpi);
buffer[3] = Convert.ToByte(gameModeEnabled);

featureRw.Write(buffer);
```

## Input/Output Reports

Consider a RGB physical push button which connects to your computer via USB. Something you might hypothetically use to acknowledge alerts. To provide this functionality the manufacturer uses input reports to raise button press/release events, and output reports for setting the RGB colour.

The manufacturer has defined the input report (button events) as follows:

| Byte  | Field         | Type      | Description
| :--   | :--           | :--       | :--
| 0     | Report ID     | `byte`    | Always `0x01`.
| 1     | Button Status | `byte`    | `1` = pressed, `0` = released.

And defines the output report (set RGB color) as follows:

| Byte  | Field     | Type      | Description
| :--   | :--       | :--       | :--
| 0     | Report ID | `byte`    | Always `0x02`.
| 1     | Red       | `byte`    | 0-255.
| 2     | Green     | `byte`    | 0-255.
| 3     | Blue      | `byte`    | 0-255.

To read input reports and write output reports, obtain a `Stream` (`System.IO`) via [IHidDevice.GetStreamAsync](docs/IHidDevice.md#GetStreamAsync).

```csharp
using var stream = await device.GetStreamAsync(HidAccessMode.ReadWrite, cancellationToken);

// Listen for button events.

var inputBuffer = new byte[device.InputReportSize];

while (true)
{
    await stream.ReadExactlyAsync(inputBuffer, cancellationToken);

    if (inputBuffer[0] == 0x01) // Report ID.
    {
        if (Convert.ToBoolean(inputBuffer[1]))
        {
            Console.WriteLine("Button was pressed");
        }
    }
}

// Set button RGB colour.

var outputBuffer = new byte[device.OutputReportSize];
outputBuffer[0] = 0x02; // Report ID.
(outputBuffer[1], outputBuffer[2], outputBuffer[3]) = (0xFF, 0x00, 0xFF); // Purple.

await stream.WriteAsync(outputBuffer, cancellationToken);
```
