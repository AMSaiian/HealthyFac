using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Defines the priorities of a work order. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class WorkOrderPriority : Enumeration<WorkOrderPriority>
{
    public static readonly WorkOrderPriority Low = new(value: "low", rank: 0);
    public static readonly WorkOrderPriority Medium = new(value: "medium", rank: 1);
    public static readonly WorkOrderPriority High = new(value: "high", rank: 2);
    public static readonly WorkOrderPriority Critical = new(value: "critical", rank: 3);

    private WorkOrderPriority(string value, int rank)
        : base(value)
    {
        Rank = rank;
    }

    /// <summary>The sort position of the priority: the higher the rank, the more urgent the work order.</summary>
    public int Rank { get; }
}
