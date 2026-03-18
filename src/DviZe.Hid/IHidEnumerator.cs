using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid;

public interface IHidEnumerator : IAsyncDisposable
{
    Task InstallAsync(CancellationToken cancellationToken = default);

    Task<IDisposable> SubscribeAsync(Action<HidEvent> callback, CancellationToken cancellationToken = default);

    Task<IDisposable> SubscribeAsync(HidEventType filter, Action<HidEvent> callback, CancellationToken cancellationToken = default);
}
