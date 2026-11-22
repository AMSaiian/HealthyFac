namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// The change that <see cref="WorkOrder.Reschedule"/> made to the schedule of a work order.
/// </summary>
public enum WorkOrderScheduleChange
{
    /// <summary>Nothing changed, and no row was added.</summary>
    None,

    /// <summary>A backlog work order got an assignee and a start.</summary>
    Assign,

    /// <summary>A scheduled work order got another assignee or start.</summary>
    Move,

    /// <summary>A scheduled work order went back to the backlog.</summary>
    Unassign,

    /// <summary>Only the estimate changed, and a row holds the new value.</summary>
    EstimateOnly,
}
