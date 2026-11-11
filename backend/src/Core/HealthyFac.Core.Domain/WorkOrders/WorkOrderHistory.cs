using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Represents one state of a work order: its status, assignee, and planned start. The row with the revision
/// <see cref="WorkOrder.CurrentRevision"/> is the current state. A row never changes after it is created. The row
/// stores no zone: the local start is a time in the zone of the company.
/// </summary>
public class WorkOrderHistory : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a row of the history.
    /// </summary>
    /// <param name="workOrderId">The work order of the row. It never changes.</param>
    /// <param name="revision">The revision of the row, from 1.</param>
    /// <param name="status">The status of the work order from this row on.</param>
    /// <param name="assigneeId">The assigned employee, or <see langword="null"/> in the backlog.</param>
    /// <param name="plannedStartLocal">
    /// The planned start as a time in the company zone, or <see langword="null"/>.
    /// </param>
    /// <param name="plannedStartUtc">The same start as a UTC instant, or <see langword="null"/>.</param>
    internal WorkOrderHistory(
        Guid workOrderId,
        int revision,
        WorkOrderStatus status,
        Guid? assigneeId,
        DateTime? plannedStartLocal,
        DateTime? plannedStartUtc
    )
    {
        WorkOrderId = workOrderId;
        Revision = revision;
        Status = status;
        AssigneeId = assigneeId;
        PlannedStartLocal = plannedStartLocal;
        PlannedStartUtc = plannedStartUtc;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid WorkOrderId { get; private set; }
    public int Revision { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public Guid? AssigneeId { get; private set; }

    public DateTime? PlannedStartLocal
    {
        get;
        private set => field = value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
    }

    public DateTime? PlannedStartUtc
    {
        get;
        private set => field = value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
