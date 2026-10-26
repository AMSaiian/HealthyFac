namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Stores a received message before the service handles it. The message ID is the primary key, so a duplicate
/// delivery adds no second row and every consumer stays idempotent.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Creates a pending message that the processor can handle at once.
    /// </summary>
    /// <param name="id">The broker message ID, which is the outbox row ID of the sending service.</param>
    /// <param name="type">The stable contract name of the message.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="receivedOn">The UTC time of the delivery.</param>
    /// <param name="traceParent">
    /// The trace context that the sender attached, or <see langword="null"/> when the sender sent none.
    /// </param>
    /// <param name="actorId">The actor from the message header, which the processor restores.</param>
    /// <param name="companyId">The company from the message header, or <see langword="null"/>.</param>
    public InboxMessage(
        Guid id,
        string type,
        string payload,
        DateTime receivedOn,
        string? traceParent,
        Guid actorId,
        Guid? companyId
    )
    {
        Id = id;
        Type = type;
        Payload = payload;
        ReceivedOn = receivedOn;
        NextAttemptOn = receivedOn;
        TraceParent = traceParent;
        ActorId = actorId;
        CompanyId = companyId;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime ReceivedOn { get; private set; }

    /// <summary>The earliest UTC time of the next handling attempt. The processor moves it after a failure.</summary>
    public DateTime NextAttemptOn { get; private set; }

    /// <summary>The UTC time of the successful handling, or <see langword="null"/> while the message waits.</summary>
    public DateTime? ProcessedOn { get; private set; }

    /// <summary>
    /// The UTC time when the message reached the attempt limit, or <see langword="null"/> while it can still be
    /// handled. The processor never claims a failed message.
    /// </summary>
    public DateTime? FailedOn { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Error { get; private set; }

    public string? TraceParent { get; private set; }

    public Guid ActorId { get; private set; }

    public Guid? CompanyId { get; private set; }

    /// <summary>
    /// Marks the message as handled. Commit it in the same <c>SaveChangesAsync</c> call as the business change,
    /// so the change and the processed state persist together or not at all.
    /// Throws <see cref="InvalidOperationException"/> when the message is already processed or failed.
    /// </summary>
    /// <param name="processedOn">The UTC time of the successful handling.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ProcessedOn"/> already holds a value, because a second handling would repeat the
    /// business change. Also thrown when <see cref="FailedOn"/> holds a value, because a failed message is never
    /// handled.
    /// </exception>
    public void MarkProcessed(DateTime processedOn)
    {
        if (ProcessedOn is not null)
        {
            throw MessagingErrors.InboxMessageAlreadyProcessed(Id);
        }

        if (FailedOn is not null)
        {
            throw MessagingErrors.InboxMessageAlreadyFailed(Id);
        }

        ProcessedOn = processedOn;
    }

    /// <summary>
    /// Records a failed handling. Below the attempt limit it moves <see cref="NextAttemptOn"/> by an exponential
    /// delay of at most five minutes. At the limit it sets <see cref="FailedOn"/> and keeps
    /// <see cref="NextAttemptOn"/>, so the processor stops claiming the message.
    /// </summary>
    /// <param name="now">The UTC time of the failure.</param>
    /// <param name="error">The text of the failure. Text beyond the column size is cut.</param>
    /// <param name="maxAttemptCount">The number of failed attempts after which the message is failed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ProcessedOn"/> already holds a value, because a handled message never retries.
    /// Also thrown when <see cref="FailedOn"/> already holds a value, because a failed message is never handled.
    /// </exception>
    public void RecordFailure(DateTime now, string error, int maxAttemptCount)
    {
        if (ProcessedOn is not null)
        {
            throw MessagingErrors.InboxMessageAlreadyProcessed(Id);
        }

        if (FailedOn is not null)
        {
            throw MessagingErrors.InboxMessageAlreadyFailed(Id);
        }

        AttemptCount++;
        Error = MessageRetryPolicy.TruncateError(error);

        if (AttemptCount >= maxAttemptCount)
        {
            FailedOn = now;
        }
        else
        {
            NextAttemptOn = MessageRetryPolicy.NextAttemptOn(now, AttemptCount);
        }
    }
}
