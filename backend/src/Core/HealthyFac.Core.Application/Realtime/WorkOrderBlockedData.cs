namespace HealthyFac.Core.Application.Realtime;

public sealed record WorkOrderBlockedData
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }
}
