using Kwerty.DviZe.Resilience;
using Kwerty.DviZe.Workers;
using System;

namespace Kwerty.DviZe.Hid.Win;

public sealed class HidOptions
{
    public bool InstallOnDemand { get; init; } = true;

    public OnDemandOptions OnDemandOptions
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = OnDemandOptions.Default;

    public RetryPolicy MetaDataRetryPolicy
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = RetryPolicy.None;

    public static HidOptions Default { get; } = new();
}
