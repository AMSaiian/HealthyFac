using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Stores a message for another service in the same transaction as the business change that causes it.
/// A background processor publishes the pending rows, so a message leaves the service only when its business
/// change commits.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Creates a pending message that the processor can publish at once.
    /// </summary>
    /// <param name="type">The stable contract name from <see cref="IIntegrationMessage.MessageType"/>.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="occurredOn">The UTC time of the business change.</param>
    /// <param name="traceParent">
    /// The trace context of the operation that created the message, or <see langword="null"/> without an active
    /// trace.
    /// </param>
    /// <param name="actorId">The user or system actor that causes the message.</param>
    /// <param name="companyId">The company of the actor, or <see langword="null"/> without one.</param>
    public OutboxMessage(
        string type,
        string payload,
        DateTime occurredOn,
        string? traceParent,
        Guid actorId,
        Guid? companyId
    )
    {
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
        NextAttemptOn = occurredOn;
        TraceParent = traceParent;
        ActorId = actorId;
        CompanyId = companyId;
    }

    /// <summary>
    /// The application generates the identifier. The processor sends it as the broker message ID, so a receiver
    /// can drop a duplicate delivery.
    /// </summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime OccurredOn { get; private set; }

    /// <summary>The earliest UTC time of the next publish attempt. The processor moves it after a failure.</summary>
    public DateTime NextAttemptOn { get; private set; }

    /// <summary>The UTC time of the successful publish, or <see langword="null"/> while the message waits.</summary>
    public DateTime? ProcessedOn { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Error { get; private set; }

    public string? TraceParent { get; private set; }

    /// <summary>The actor that the processor sends in a header, so the receiver restores it.</summary>
    public Guid ActorId { get; private set; }

    public Guid? CompanyId { get; private set; }

    /// <summary>
    /// Marks the message as published.
    /// </summary>
    /// <param name="publishedOn">The UTC time of the successful publish.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ProcessedOn"/> already holds a value, because a second publish would send the
    /// message twice.
    /// </exception>
    public void MarkPublished(DateTime publishedOn)
    {
        EnsurePending();

        ProcessedOn = publishedOn;
    }

    /// <summary>
    /// Records a failed publish and moves <see cref="NextAttemptOn"/> by an exponential delay of at most five
    /// minutes.
    /// </summary>
    /// <param name="now">The UTC time of the failure.</param>
    /// <param name="error">The text of the failure. Text beyond the column size is cut.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ProcessedOn"/> already holds a value, because a published message never retries.
    /// </exception>
    public void ScheduleRetry(DateTime now, string error)
    {
        EnsurePending();

        AttemptCount++;
        Error = MessageRetryPolicy.TruncateError(error);
        NextAttemptOn = MessageRetryPolicy.NextAttemptOn(now, AttemptCount);
    }

    private void EnsurePending()
    {
        if (ProcessedOn is not null)
        {
            throw MessagingErrors.OutboxMessageAlreadyProcessed(Id);
        }
    }
}
