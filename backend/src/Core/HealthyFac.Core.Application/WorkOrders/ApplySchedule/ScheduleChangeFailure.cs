namespace HealthyFac.Core.Application.WorkOrders.ApplySchedule;

/// <summary>One rejected change of an apply.</summary>
public sealed record ScheduleChangeFailure
{
    public required Guid WorkOrderId { get; init; }

    public required ScheduleChangeRejection Reason { get; init; }
}
