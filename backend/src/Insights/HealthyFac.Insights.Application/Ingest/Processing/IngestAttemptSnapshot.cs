namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// The values of a claimed attempt that the pipeline reads. The pipeline never holds the tracked entity.
/// </summary>
public record IngestAttemptSnapshot
{
    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required DateTime QueuedAt { get; init; }

    public string? TraceParent { get; init; }
}
