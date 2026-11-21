namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// The assignment of a work order before and after a change, with the identity users of the assignees. A use case
/// reads the users inside its transaction operation and returns this record, so the code after the commit only adds
/// events.
/// </summary>
public sealed record WorkOrderAssignmentChange
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    public Guid? OldAssigneeId { get; init; }

    public Guid? OldAssigneeUserId { get; init; }

    public Guid? NewAssigneeId { get; init; }

    public Guid? NewAssigneeUserId { get; init; }

    /// <summary>The new planned start. It is <see langword="null"/> when the work order has no assignee.</summary>
    public DateTime? PlannedStartLocal { get; init; }
}
