namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// The queued attempts at the time of a sample: how many wait and since when the oldest waits.
/// </summary>
public record IngestBacklog
{
    public static readonly IngestBacklog Empty = new() { Count = 0, OldestQueuedAt = null };

    public required int Count { get; init; }

    public DateTime? OldestQueuedAt { get; init; }
}
