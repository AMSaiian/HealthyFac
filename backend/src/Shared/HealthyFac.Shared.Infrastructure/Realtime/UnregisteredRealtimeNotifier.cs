using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Shared.Infrastructure.Realtime;

/// <summary>
/// The notifier of a service that never called <c>AddRealtimeNotifier</c>. The inbox runner needs a notifier for each
/// message, so this one lets it run, and it throws when a handler adds an event, because no queue would deliver it.
/// </summary>
internal sealed class UnregisteredRealtimeNotifier : IRealtimeNotifier
{
    public void AddToUser(Guid userId, RealtimeNotificationKind kind, object data) =>
        throw RealtimeErrors.NotRegistered();

    public void AddToGroup(Guid companyId, RealtimeNotificationKind kind, object data) =>
        throw RealtimeErrors.NotRegistered();

    public void AddHint(Guid companyId, RealtimeHint hint) => throw RealtimeErrors.NotRegistered();

    public void AddSessionEnd(Guid userId) => throw RealtimeErrors.NotRegistered();

    public void Clear()
    {
    }

    public void Push()
    {
    }
}
