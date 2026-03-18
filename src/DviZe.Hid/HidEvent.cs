namespace Kwerty.DviZe.Hid;

public sealed class HidEvent(IHidDevice device, HidEventType eventType)
{
    public IHidDevice Device => device;

    public HidEventType EventType => eventType;
}