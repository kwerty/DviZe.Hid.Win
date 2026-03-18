# DviZe.Hid.Win

A .NET 10 class library for enumerating and communicating with HID devices in Windows.

## Enumerating

Create an [HidEnumerator](docs/HidEnumerator.md) and call [SubscribeAsync](docs/HidEnumerator.md#SubscribeAsync) with a callback to receive [HidEvent](docs/HidEnumerator.md#HidEvent) notifications.

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

For this example, imagine a gaming mouse whose configuration can be read and/or written via a feature report.

The manufacturer has defined the feature report as follows:

| Byte(s) | Field | Type | Description
| --- | --- | --- | ---
| 0 | Report ID | `byte` | Always `0x03`.
| 1-2 | Mouse DPI | `uint16` (Little endian) | DPI value between 1000-5000.
| 3 | Game Mode | `byte` | `1` = enabled, `0` = disabled.

We'll use an [IHidFeatureReportReaderWriter](docs/IHidFeatureReportReaderWriter.md) which we obtain via [IHidDevice](docs/IHidDevice.md).[GetFeatureReportReaderWriterAsync](docs/IHidDevice.md#GetFeatureReportReaderWriterAsync).

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

For this example, imagine an RGB physical push button which connects to your computer via USB. Something you might hypothetically use to acknowledge alerts. To provide this functionality the manufacturer uses input reports to raise button press/release events, and output reports for setting the RGB colour.

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

We'll obtain a `Stream` (`System.IO`) by calling [IHidDevice](docs/IHidDevice.md).[GetStreamAsync](docs/IHidDevice.md#GetStreamAsync).

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
        else
        {
            Console.WriteLine("Button was released");
        }
    }
}

// Set button RGB colour.

var outputBuffer = new byte[device.OutputReportSize];
outputBuffer[0] = 0x02; // Report ID.
(outputBuffer[1], outputBuffer[2], outputBuffer[3]) = (0xFF, 0x00, 0xFF); // Purple.

await stream.WriteAsync(outputBuffer, cancellationToken);
```
