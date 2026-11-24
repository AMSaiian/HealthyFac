using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.WorkOrders;

public sealed class WorkOrderSeed
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    /// <summary>The key of the machine in <c>machines.json</c>.</summary>
    public required string Machine { get; init; }

    public required int EstimateMinutes { get; init; }

    public required RelativeMoment CreatedAt { get; init; }

    public required string CreatedBy { get; init; }

    public AssignmentSeed? Assignment { get; init; }

    public IReadOnlyList<StatusChangeSeed> StatusChanges { get; init; } = [];
}
