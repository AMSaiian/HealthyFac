using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Links a work order to a file of the Document service. A removal keeps the row (soft delete).
/// </summary>
public class WorkOrderFile : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a link.
    /// </summary>
    /// <param name="workOrderId">The work order of the link. It never changes.</param>
    /// <param name="fileId">The file of the Document service. It never changes.</param>
    internal WorkOrderFile(Guid workOrderId, Guid fileId)
    {
        WorkOrderId = workOrderId;
        FileId = fileId;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid WorkOrderId { get; private set; }
    public Guid FileId { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
