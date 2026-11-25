using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// The work order that handles a drain. The job derives the rest from the drain: the creator, the assignee, the
/// status changes, and the link moment.
/// </summary>
public sealed class DrainWorkOrderSeed
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public required int EstimateMinutes { get; init; }

    /// <summary>The moment of the creation, in the hour form. The assignment follows 5 minutes later.</summary>
    public required RelativeMoment CreatedAt { get; init; }
}
