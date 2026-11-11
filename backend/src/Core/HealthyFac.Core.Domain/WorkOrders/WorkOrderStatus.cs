using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Defines the statuses of a work order. The camelCase values reach the API and the database, so never rename one in
/// place. Every status except <see cref="Backlog"/> belongs to a work order that has an assignee and a planned start.
/// </summary>
public sealed class WorkOrderStatus : Enumeration<WorkOrderStatus>
{
    public static readonly WorkOrderStatus Backlog = new(value: "backlog");
    public static readonly WorkOrderStatus Staged = new(value: "staged");
    public static readonly WorkOrderStatus InProgress = new(value: "inProgress");
    public static readonly WorkOrderStatus Blocked = new(value: "blocked");
    public static readonly WorkOrderStatus Completed = new(value: "completed");
    public static readonly WorkOrderStatus Rejected = new(value: "rejected");

    private WorkOrderStatus(string value)
        : base(value)
    {
    }

    public bool IsScheduled => this != Backlog;
}
