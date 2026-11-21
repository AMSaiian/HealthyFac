namespace HealthyFac.Core.Application.Realtime;

public sealed record WorkOrderRescheduledData
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    public required DateTime PlannedStartLocal { get; init; }
}
