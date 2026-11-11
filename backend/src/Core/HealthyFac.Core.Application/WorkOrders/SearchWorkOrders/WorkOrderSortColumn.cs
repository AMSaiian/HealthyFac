using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

/// <summary>
/// The sortable columns of the work order grid.
/// </summary>
public sealed class WorkOrderSortColumn : Enumeration<WorkOrderSortColumn>
{
    public static readonly WorkOrderSortColumn Title = new(value: "title");
    public static readonly WorkOrderSortColumn Priority = new(value: "priority");
    public static readonly WorkOrderSortColumn PlannedStart = new(value: "plannedStart");
    public static readonly WorkOrderSortColumn CreatedAt = new(value: "createdAt");
    public static readonly WorkOrderSortColumn IsDelayed = new(value: "isDelayed");

    private WorkOrderSortColumn(string value)
        : base(value)
    {
    }
}
