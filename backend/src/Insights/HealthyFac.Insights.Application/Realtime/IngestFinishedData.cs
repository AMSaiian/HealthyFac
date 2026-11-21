using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.Application.Realtime;

public sealed record IngestFinishedData
{
    public required Guid AttemptId { get; init; }

    public required string FileName { get; init; }

    public required IngestState State { get; init; }
}
