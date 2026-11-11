namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// An attempt that the worker claimed: the identity to run it as.
/// </summary>
public record ClaimedAttempt
{
    public required Guid AttemptId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>The user who queued the attempt. The run restores this actor.</summary>
    public required Guid QueuedBy { get; init; }
}
