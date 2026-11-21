namespace HealthyFac.Core.Application.Realtime;

public sealed record WorkOrderEstimateChangedData
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    public required TimeSpan OldEstimate { get; init; }

    public required TimeSpan NewEstimate { get; init; }
}
