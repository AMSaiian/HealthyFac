namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// The place of a scheduled work order: its assignee and its planned start. The two values always come together, so a
/// work order never holds an assignee without a start.
/// </summary>
public sealed record WorkOrderSlot
{
    public required Guid AssigneeId { get; init; }

    /// <summary>The planned start as a time in the company zone.</summary>
    public required DateTime PlannedStartLocal { get; init; }

    /// <summary>The same start as a UTC instant.</summary>
    public required DateTime PlannedStartUtc { get; init; }
}
