namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// The result of an update of a work order, for its real-time events. A use case returns it from its transaction
/// operation, with the identity user of the assignee read inside the operation.
/// </summary>
public sealed record WorkOrderEstimateChange
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// The web user of the assignee, or <see langword="null"/> without an assignee or an identity user.
    /// </summary>
    public Guid? AssigneeUserId { get; init; }

    public required TimeSpan OldEstimate { get; init; }

    public required TimeSpan NewEstimate { get; init; }
}
