namespace Kwerty.DviZe.Hid;

public sealed class HidHandleOptions
{
    public HidHandleOptions()
    {
    }

    public HidHandleOptions(HidAccessMode accessMode)
    {
        AccessMode = accessMode;
    }

    public HidAccessMode AccessMode { get; init; }

    public bool ExclusiveRead { get; init; }

    public bool ExclusiveWrite { get; init; }
}
