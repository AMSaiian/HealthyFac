using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;

namespace HealthyFac.Shared.Infrastructure.Realtime;

/// <summary>
/// Ties the real-time events of an inbox handler to the unit of the inbox runner. Each attempt of the unit starts
/// with an empty notifier, so a repeated attempt adds its events once. After the commit, the events go to the
/// background queue. A rolled-back unit pushes nothing.
/// </summary>
/// <param name="notifier">The scoped notifier that the handler of the message adds its events to.</param>
public sealed class RealtimeInboxObserver(IRealtimeNotifier notifier) : IInboxUnitObserver
{
    private readonly IRealtimeNotifier _notifier = notifier;

    public void OnUnitStarting() => _notifier.Clear();

    public void OnUnitCommitted() => _notifier.Push();
}
