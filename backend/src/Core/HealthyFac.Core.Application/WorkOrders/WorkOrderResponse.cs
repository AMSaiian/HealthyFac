using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// The work order as the API shows it: the fields, the current status with the assignee and the planned start, the
/// machine, and the file IDs.
/// </summary>
public record WorkOrderResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public required int EstimateMinutes { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStartLocal { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    /// <summary>True for a staged work order whose planned start is before now.</summary>
    public required bool IsDelayed { get; init; }

    public Guid? MachineId { get; init; }

    public string? MachineName { get; init; }

    /// <summary>
    /// True when the machine was deleted. The name stays, so the work order still shows what it was for.
    /// </summary>
    public required bool IsMachineDeleted { get; init; }

    public required IReadOnlyList<Guid> FileIds { get; init; }

    public required uint RowVersion { get; init; }

    public required DateTime CreatedAt { get; init; }
}
