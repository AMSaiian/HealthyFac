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
    public InboxMessage(Guid id, string type, string payload, DateTime receivedOn, string? traceParent)
    {
        Id = id;
        Type = type;
        Payload = payload;
        ReceivedOn = receivedOn;
        NextAttemptOn = receivedOn;
        TraceParent = traceParent;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime ReceivedOn { get; private set; }

    /// <summary>The earliest UTC time of the next handling attempt. The processor moves it after a failure.</summary>
    public DateTime NextAttemptOn { get; private set; }

    /// <summary>The UTC time of the successful handling, or <see langword="null"/> while the message waits.</summary>
    public DateTime? ProcessedOn { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Error { get; private set; }

    public string? TraceParent { get; private set; }
}
