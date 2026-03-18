using Kwerty.DviZe.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid.Win;

public sealed class HidEnumerator : IHidEnumerator
{
    readonly HidOptions options;
    readonly ILoggerFactory loggerFactory;
    readonly IWorkerProvider<HidEnumeratorSession> sessionProvider;
    readonly Runner<HidSubscriptionWorker> subscriptionRunner;

    public HidEnumerator(ILoggerFactory loggerFactory)
        : this(HidOptions.Default, loggerFactory)
    {
    }

    public HidEnumerator(HidOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        this.options = options;
        this.loggerFactory = loggerFactory;
        sessionProvider = options.InstallOnDemand
            ? new OnDemand<HidEnumeratorSession>(options.OnDemandOptions, CreateSession, loggerFactory)
            : new RunSingle<HidEnumeratorSession>(loggerFactory);
        subscriptionRunner = new Runner<HidSubscriptionWorker>(loggerFactory);
    }

    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        if (sessionProvider is not RunSingle<HidEnumeratorSession> sessionRunner)
        {
            throw new InvalidOperationException();
        }

        await sessionRunner.StartWorkerAsync(CreateSession(), cancellationToken).ConfigureAwait(false);
    }

    public Task<IDisposable> SubscribeAsync(Action<HidEvent> callback, CancellationToken cancellationToken = default)
        => SubscribeAsyncCore(new HidSubscription(filter: null, callback), cancellationToken);

    public Task<IDisposable> SubscribeAsync(HidEventType filter, Action<HidEvent> callback, CancellationToken cancellationToken = default)
        => SubscribeAsyncCore(new HidSubscription(filter, callback), cancellationToken);

    async Task<IDisposable> SubscribeAsyncCore(HidSubscription subscription, CancellationToken cancellationToken)
    {
        var worker = new HidSubscriptionWorker(subscription, sessionProvider);
        await subscriptionRunner.StartWorkerAsync(worker, cancellationToken).ConfigureAwait(false);
        return worker;
    }

    public async ValueTask DisposeAsync()
    {
        await subscriptionRunner.DisposeAsync().ConfigureAwait(false);
        await ((IAsyncDisposable)sessionProvider).DisposeAsync().ConfigureAwait(false); // Both OnDemand/RunSingle require async disposal.
    }

    HidEnumeratorSession CreateSession() => new(options, loggerFactory);
}