namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// One work order that a late completion moved or sent back to the backlog.
/// </summary>
public sealed record WorkOrderSlideMove
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    /// <summary>The web user of the assignee, or <see langword="null"/> when the assignee has none.</summary>
    public Guid? AssigneeUserId { get; init; }

    /// <summary>The new local start, or <see langword="null"/> when the work order went to the backlog.</summary>
    public DateTime? NewStartLocal { get; init; }
}
