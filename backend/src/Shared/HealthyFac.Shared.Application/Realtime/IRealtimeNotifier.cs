namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Collects the real-time events of one unit of work and hands them to the background queue. The notifier is scoped,
/// so a handler and the code that runs the handler share one buffer.
/// </summary>
/// <remarks>
/// Add events only after the save returned (<c>SaveChangesAsync</c> or <c>RunInTransactionAsync</c>), never inside
/// the operation of a transaction: the execution strategy repeats that operation. <see cref="Push"/> only
/// enqueues the events, so the request does not wait for Redis.
/// </remarks>
public interface IRealtimeNotifier
{
    /// <summary>
    /// Adds a notification of a user-targeted kind for one user. The event is skipped when the current actor is that
    /// user, because the user already knows the result of their own action.
    /// </summary>
    /// <param name="userId">The <c>sub</c> of the receiving user.</param>
    /// <param name="kind">A user-targeted kind.</param>
    /// <param name="data">The data of the kind. It holds plain data only.</param>
    void AddToUser(Guid userId, RealtimeNotificationKind kind, object data);

    /// <summary>
    /// Adds a notification of a group kind for every user of the company who holds the permission of the kind.
    /// </summary>
    /// <param name="companyId">The company of the group.</param>
    /// <param name="kind">A group kind (<see cref="RealtimeAudiences.KindPermissions"/>).</param>
    /// <param name="data">The data of the kind. It holds plain data only.</param>
    void AddToGroup(Guid companyId, RealtimeNotificationKind kind, object data);

    /// <summary>
    /// Adds a hint for every user of the company who holds the permission of the topic.
    /// </summary>
    /// <param name="companyId">The company of the topic group.</param>
    /// <param name="hint">The hint.</param>
    void AddHint(Guid companyId, RealtimeHint hint);

    /// <summary>
    /// Adds the end of the web sessions of a user.
    /// </summary>
    /// <param name="userId">The <c>sub</c> of the user.</param>
    void AddSessionEnd(Guid userId);

    /// <summary>
    /// Drops the buffered events. A transaction operation calls it first, because the execution strategy can repeat
    /// the operation.
    /// </summary>
    void Clear();

    /// <summary>
    /// Enqueues each buffered event and empties the buffer. The method only enqueues: it waits for nothing and does
    /// no I/O, so it takes no token.
    /// </summary>
    void Push();
}
