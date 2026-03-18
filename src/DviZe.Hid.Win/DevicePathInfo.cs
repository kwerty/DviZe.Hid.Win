using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class DevicePathInfo
{
    public DevicePathInfo(string devicePath)
    {
        var match = Regex.Match(devicePath, @"^^\\\\\?\\HID#(.+)#(.+)#");

        if (match.Success)
        {
            DevicePath = devicePath;

            var deviceId = match.Groups[1].Value;
            InstanceId = match.Groups[2].Value;

            var vidMatch = Regex.Match(deviceId, @"VID_([a-fA-F0-9]{4})");
            var pidMatch = Regex.Match(deviceId, @"PID_([a-fA-F0-9]{4})");
            var miMatch = Regex.Match(deviceId, @"MI_([a-fA-F0-9]{2})");

            if (vidMatch.Success
                && pidMatch.Success)
            {
                VendorId = int.Parse(vidMatch.Groups[1].Value, NumberStyles.HexNumber);
                ProductId = int.Parse(pidMatch.Groups[1].Value, NumberStyles.HexNumber);

                if (miMatch.Success)
                {
                    InterfaceNumber = int.Parse(miMatch.Groups[1].Value, NumberStyles.HexNumber);
                }

                return;
            }
        }

        throw new ArgumentException($"Invalid device path:\r\n{devicePath}", nameof(devicePath));
    }

    public string DevicePath { get; }

    public string InstanceId { get; }

    public int VendorId { get; }

    public int ProductId { get; }

    public int? InterfaceNumber { get; }
}
