namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Stores received messages before the service handles them, so every consumer stays idempotent.
/// </summary>
public interface IInbox
{
    /// <summary>
    /// Stores a received message unless the inbox already holds its ID. The insert runs at once with
    /// <c>ON CONFLICT DO NOTHING</c>, so a duplicate delivery neither throws nor aborts an open transaction.
    /// </summary>
    /// <param name="messageId">The broker message ID, which is the outbox row ID of the sending service.</param>
    /// <param name="type">The stable contract name of the message.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="traceParent">
    /// The trace context that the sender attached, or <see langword="null"/> when the sender sent none.
    /// </param>
    /// <param name="actorId">The actor from the message header, which the processor restores.</param>
    /// <param name="companyId">The company from the message header, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>
    /// <see langword="true"/> when the inbox stored the message; <see langword="false"/> when it already held the
    /// message ID.
    /// </returns>
    Task<bool> TryAddAsync(
        Guid messageId,
        string type,
        string payload,
        string? traceParent,
        Guid actorId,
        Guid? companyId,
        CancellationToken cancellationToken = default(CancellationToken)
    );

    /// <summary>
    /// Marks <paramref name="message"/> as handled on the scoped service context. The next
    /// <c>SaveChangesAsync</c> call commits the processed state together with the business change of the
    /// handler. Throws <see cref="InvalidOperationException"/> when the scoped context does not track the message.
    /// </summary>
    /// <param name="message">A message loaded through the inbox set of the scoped service context.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scoped service context does not track <paramref name="message"/>. The processed state would
    /// then never save, and the processor would handle the message again.
    /// </exception>
    void MarkProcessed(InboxMessage message);
}
