using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Shared.Infrastructure.Realtime;

/// <summary>
/// Buffers the real-time events of one scope. Each event keeps the trace context of the code that added it, so the
/// push span of the sender links to that operation.
/// </summary>
/// <param name="currentUserProvider">Reads the actor of the operation, for the own action rule.</param>
/// <param name="timeProvider">The clock for the time of a notification.</param>
/// <param name="queue">The queue that the background sender reads.</param>
internal sealed class RealtimeNotifier(
    ICurrentUserProvider currentUserProvider,
    TimeProvider timeProvider,
    IBackgroundQueue<RealtimePush> queue
) : IRealtimeNotifier
{
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IBackgroundQueue<RealtimePush> _queue = queue;
    private readonly List<(RealtimePush Push, ActivityContext Context)> _buffer = [];

    public void AddToUser(Guid userId, RealtimeNotificationKind kind, object data)
    {
        if (RealtimeAudiences.KindPermissions.ContainsKey(kind))
        {
            throw RealtimeErrors.KindNotUserTargeted(kind);
        }

        var actor = _currentUserProvider.TryGetActor();
        var isOwnAction = actor?.Kind == ActorKind.User && actor.UserId == userId;
        if (isOwnAction)
        {
            return;
        }

        Add(
            new RealtimePush
            {
                Target = RealtimePushTarget.User,
                UserId = userId,
                Notification = CreateNotification(kind, data, actor),
                MetricTarget = kind.Value,
            });
    }

    public void AddToGroup(Guid companyId, RealtimeNotificationKind kind, object data)
    {
        if (!RealtimeAudiences.KindPermissions.ContainsKey(kind))
        {
            throw RealtimeErrors.KindNotGrouped(kind);
        }

        Add(
            new RealtimePush
            {
                Target = RealtimePushTarget.Group,
                Group = RealtimeGroups.ForKind(companyId, kind),
                Notification = CreateNotification(kind, data, _currentUserProvider.TryGetActor()),
                MetricTarget = kind.Value,
            });
    }

    public void AddHint(Guid companyId, RealtimeHint hint) =>
        Add(
            new RealtimePush
            {
                Target = RealtimePushTarget.Group,
                Group = RealtimeGroups.ForTopic(companyId, hint.Topic),
                Hint = hint,
                MetricTarget = hint.Topic.Value,
            });

    public void AddSessionEnd(Guid userId) =>
        Add(
            new RealtimePush
            {
                Target = RealtimePushTarget.User,
                UserId = userId,
                IsSessionEnd = true,
                MetricTarget = ObservabilityConstants.Realtime.SessionEnded,
            });

    public void Clear() => _buffer.Clear();

    public void Push()
    {
        foreach (var (push, context) in _buffer)
        {
            _queue.Enqueue(push, context);
        }

        _buffer.Clear();
    }

    private void Add(RealtimePush push) => _buffer.Add((push, Activity.Current?.Context ?? default(ActivityContext)));

    private RealtimeNotification CreateNotification(RealtimeNotificationKind kind, object data, ActorContext? actor)
    {
        var now = _timeProvider.GetUtcNow();

        return new RealtimeNotification
        {
            NotificationId = Guid.CreateVersion7(now),
            Kind = kind,
            OccurredAt = now,
            ActorSubject = actor?.Kind == ActorKind.User ? actor.UserId : null,
            Data = data,
        };
    }
}
