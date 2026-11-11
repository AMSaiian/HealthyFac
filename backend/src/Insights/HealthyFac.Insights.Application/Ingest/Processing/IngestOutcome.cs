using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// How a run ended: the final state and the reason of a failure or an interrupt.
/// </summary>
public record IngestOutcome
{
    public required IngestState State { get; init; }

    public IngestErrorCode? ErrorCode { get; init; }

    public IngestInterruptReason? InterruptReason { get; init; }
}
