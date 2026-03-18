using System;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidSubscription
{
    readonly HidEventType? filter;
    readonly internal Action<HidEvent> callback;

    internal HidSubscription(HidEventType? filter, Action<HidEvent> callback)
    {
        this.filter = filter;
        this.callback = callback;
    }

    internal bool IsEligibleForEvent(HidEventType eventType)
    {
        return !filter.HasValue
            || filter.Value == eventType;
    }
}