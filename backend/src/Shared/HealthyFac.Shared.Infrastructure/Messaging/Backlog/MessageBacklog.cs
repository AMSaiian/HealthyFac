namespace HealthyFac.Shared.Infrastructure.Messaging.Backlog;

/// <summary>
/// The last sample of the pending outbox and inbox rows, which the backlog gauges report.
/// </summary>
internal sealed record MessageBacklog
{
    public required PendingBacklog Outbox { get; init; }

    public required PendingBacklog Inbox { get; init; }
}
