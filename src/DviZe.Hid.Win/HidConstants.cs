namespace Kwerty.DviZe.Hid.Win;

internal static class HidConstants
{
    // The Hidapi source notes a Win32 bug where certain USB devices may return broken strings
    // if the buffer size is 127 WCHARs or larger. This would affect our Win32 calls to read
    // the vendor name, product name and serial number.
    // Hopefully the bug was fixed, but if not, we should use the same workaround as Hidapi.
    public static readonly int StringDescriptorMaxBytes = 512;
}
