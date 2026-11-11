namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Defines the exceptions that the state rules of a work order throw. The Application layer maps them to a rule
/// rejection.
/// </summary>
public static class WorkOrderStateErrors
{
    public static InvalidOperationException NotInBacklog(Guid workOrderId, WorkOrderStatus status) =>
        new($"Work order '{workOrderId}' is in status '{status.Value}', so it cannot be assigned.");

    public static InvalidOperationException NotScheduled(Guid workOrderId) =>
        new($"Work order '{workOrderId}' is in the backlog, so it has no assignee and no planned start.");

    public static InvalidOperationException StatusIsBacklog(Guid workOrderId) =>
        new($"Work order '{workOrderId}' cannot change its status to backlog. Unassign it instead.");

    public static InvalidOperationException SameStatus(Guid workOrderId, WorkOrderStatus status) =>
        new($"Work order '{workOrderId}' is already in status '{status.Value}'.");
}
