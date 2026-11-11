using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.WorkOrders;

/// <summary>
/// Represents a maintenance work order of one company. Its status, assignee, and planned start live in the append-only
/// <see cref="History"/>: each change of them adds a row with the next revision and sets
/// <see cref="CurrentRevision"/> to it. A removal keeps the row (soft delete).
/// </summary>
public class WorkOrder : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private readonly List<WorkOrderHistory> _history = [];
    private readonly List<WorkOrderFile> _files = [];

    /// <summary>
    /// Fills the data of a work order. Entity Framework Core binds it when it loads a row, so it adds no history row.
    /// </summary>
    /// <param name="companyId">The company of the work order. It never changes.</param>
    /// <param name="title">The title.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="category">The category.</param>
    /// <param name="machineId">The machine of the work order, or <see langword="null"/>.</param>
    /// <param name="estimate">The planned working time.</param>
    private WorkOrder(
        Guid companyId,
        string title,
        string? description,
        WorkOrderPriority priority,
        WorkOrderCategory category,
        Guid? machineId,
        TimeSpan estimate
    )
    {
        CompanyId = companyId;
        Title = title;
        Description = description;
        Priority = priority;
        Category = category;
        MachineId = machineId;
        Estimate = estimate;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Title { get; private set; }

    /// <summary>The description as HTML. The Application layer checks it against the rich text policy.</summary>
    public string? Description { get; private set; }

    public WorkOrderPriority Priority { get; private set; }
    public WorkOrderCategory Category { get; private set; }
    public Guid? MachineId { get; private set; }
    public TimeSpan Estimate { get; private set; }

    /// <summary>The revision of the newest history row.</summary>
    public int CurrentRevision { get; private set; }

    public IReadOnlyCollection<WorkOrderHistory> History => _history;
    public IReadOnlyCollection<WorkOrderFile> Files => _files;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Creates a work order in the backlog (revision 1).
    /// </summary>
    /// <param name="companyId">The company of the work order. It never changes.</param>
    /// <param name="title">The title.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="category">The category.</param>
    /// <param name="machineId">The machine of the work order, or <see langword="null"/>.</param>
    /// <param name="estimate">The planned working time.</param>
    /// <returns>The new work order with its first history row.</returns>
    public static WorkOrder Create(
        Guid companyId,
        string title,
        string? description,
        WorkOrderPriority priority,
        WorkOrderCategory category,
        Guid? machineId,
        TimeSpan estimate
    )
    {
        var workOrder = new WorkOrder(companyId, title, description, priority, category, machineId, estimate);
        workOrder.AddRow(WorkOrderStatus.Backlog, assigneeId: null, plannedStartLocal: null, plannedStartUtc: null);

        return workOrder;
    }

    /// <summary>
    /// The history row with the revision <see cref="CurrentRevision"/>. The caller must load that row.
    /// </summary>
    /// <exception cref="InvalidOperationException">The history holds no row with the current revision.</exception>
    public WorkOrderHistory Current => _history.Single(row => row.Revision == CurrentRevision);

    /// <summary>
    /// Changes the data that a user edits. It adds no history row, because the status, the assignee, and the planned
    /// start stay as they are.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="category">The category.</param>
    /// <param name="machineId">The machine of the work order, or <see langword="null"/>.</param>
    public void Update(
        string title,
        string? description,
        WorkOrderPriority priority,
        WorkOrderCategory category,
        Guid? machineId
    )
    {
        Title = title;
        Description = description;
        Priority = priority;
        Category = category;
        MachineId = machineId;
    }

    /// <summary>
    /// Changes the planned working time. It adds no history row. The caller checks that the slot still fits.
    /// </summary>
    /// <param name="estimate">The new planned working time.</param>
    public void ChangeEstimate(TimeSpan estimate)
    {
        Estimate = estimate;
    }

    /// <summary>
    /// Assigns a backlog work order: adds a row in status <see cref="WorkOrderStatus.Staged"/>.
    /// </summary>
    /// <remarks>
    /// The caller computes <paramref name="plannedStartUtc"/> from the local start and the company zone.
    /// </remarks>
    /// <param name="assigneeId">The assigned employee.</param>
    /// <param name="plannedStartLocal">The planned start as a time in the company zone.</param>
    /// <param name="plannedStartUtc">The same start as a UTC instant.</param>
    /// <exception cref="InvalidOperationException">The current status is not backlog.</exception>
    public void Assign(Guid assigneeId, DateTime plannedStartLocal, DateTime plannedStartUtc)
    {
        var status = Current.Status;
        if (status != WorkOrderStatus.Backlog)
        {
            throw WorkOrderStateErrors.NotInBacklog(Id, status);
        }

        AddRow(WorkOrderStatus.Staged, assigneeId, plannedStartLocal, plannedStartUtc);
    }

    /// <summary>
    /// Moves a scheduled work order to another assignee or start: adds a row that keeps the status.
    /// </summary>
    /// <param name="assigneeId">The assigned employee.</param>
    /// <param name="plannedStartLocal">The planned start as a time in the company zone.</param>
    /// <param name="plannedStartUtc">The same start as a UTC instant.</param>
    /// <exception cref="InvalidOperationException">The work order is in the backlog.</exception>
    public void Move(Guid assigneeId, DateTime plannedStartLocal, DateTime plannedStartUtc)
    {
        var status = EnsureScheduled();

        AddRow(status, assigneeId, plannedStartLocal, plannedStartUtc);
    }

    /// <summary>
    /// Sends a scheduled work order back to the backlog: adds a row without assignee and planned start.
    /// </summary>
    /// <exception cref="InvalidOperationException">The work order is in the backlog.</exception>
    public void Unassign()
    {
        EnsureScheduled();

        AddRow(WorkOrderStatus.Backlog, assigneeId: null, plannedStartLocal: null, plannedStartUtc: null);
    }

    /// <summary>
    /// Changes the status of a scheduled work order: adds a row that copies the assignee and the planned start.
    /// </summary>
    /// <param name="status">The new status. It is never backlog and never the current status.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="status"/> is backlog or the current status, or the work order is in the backlog.
    /// </exception>
    public void ChangeStatus(WorkOrderStatus status)
    {
        var current = Current;
        if (status == WorkOrderStatus.Backlog)
        {
            throw WorkOrderStateErrors.StatusIsBacklog(Id);
        }

        if (current.Status == WorkOrderStatus.Backlog)
        {
            throw WorkOrderStateErrors.NotScheduled(Id);
        }

        if (status == current.Status)
        {
            throw WorkOrderStateErrors.SameStatus(Id, status);
        }

        AddRow(status, current.AssigneeId, current.PlannedStartLocal, current.PlannedStartUtc);
    }

    /// <summary>
    /// Makes the file links match <paramref name="fileIds"/>: it adds a link for each new ID and takes the links of
    /// the missing IDs out of <see cref="Files"/>. An unchanged list changes nothing.
    /// </summary>
    /// <remarks>
    /// The caller loads the existing links first and calls <c>Remove</c> on each returned link, so the soft delete
    /// stores the removal.
    /// </remarks>
    /// <param name="fileIds">The IDs of the files that the work order links to after the call.</param>
    /// <returns>The links that left <see cref="Files"/>.</returns>
    public IReadOnlyCollection<WorkOrderFile> ReplaceFiles(IReadOnlyCollection<Guid> fileIds)
    {
        var wantedIds = fileIds.ToHashSet();
        var removedLinks = _files.Where(link => !wantedIds.Contains(link.FileId)).ToList();
        _files.RemoveAll(removedLinks.Contains);
        var existingIds = _files.Select(link => link.FileId).ToHashSet();
        _files.AddRange(
            wantedIds.Where(fileId => !existingIds.Contains(fileId)).Select(fileId => new WorkOrderFile(Id, fileId)));

        return removedLinks;
    }

    private WorkOrderStatus EnsureScheduled()
    {
        var status = Current.Status;

        return status.IsScheduled ? status : throw WorkOrderStateErrors.NotScheduled(Id);
    }

    private void AddRow(
        WorkOrderStatus status,
        Guid? assigneeId,
        DateTime? plannedStartLocal,
        DateTime? plannedStartUtc
    )
    {
        var revision = CurrentRevision + 1;
        _history.Add(new WorkOrderHistory(Id, revision, status, assigneeId, plannedStartLocal, plannedStartUtc));
        CurrentRevision = revision;
    }
}
