using Kwerty.DviZe.Workers;
using System;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Hid.Win;

internal sealed class HidSubscriptionWorker(HidSubscription subscription, IWorkerProvider<HidEnumeratorSession> sessionProvider)
    : Worker, IDisposable
{
    HidEnumeratorSession session;
    IDisposable sessionReleaser;

    protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
    {
        (session, sessionReleaser) = await sessionProvider.LeaseAsync(startingContext.CancellationToken).ConfigureAwait(false);
        session.AddSubscription(subscription);
    }

    protected override Task OnStoppingAsync()
    {
        session.RemoveSubscription(subscription);
        sessionReleaser.Dispose();
        return Task.CompletedTask;
    }

    void IDisposable.Dispose() => Context.TryStop();
}