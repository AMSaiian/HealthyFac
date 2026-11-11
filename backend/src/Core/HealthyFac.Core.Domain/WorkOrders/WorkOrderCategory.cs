using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Defines the categories of a work order. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class WorkOrderCategory : Enumeration<WorkOrderCategory>
{
    public static readonly WorkOrderCategory Inspection = new(value: "inspection");
    public static readonly WorkOrderCategory Preventive = new(value: "preventive");
    public static readonly WorkOrderCategory Repair = new(value: "repair");
    public static readonly WorkOrderCategory Other = new(value: "other");

    private WorkOrderCategory(string value)
        : base(value)
    {
    }
}
