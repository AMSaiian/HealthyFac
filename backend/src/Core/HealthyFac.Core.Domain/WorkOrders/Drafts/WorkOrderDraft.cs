using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.WorkOrders.Drafts;

/// <summary>
/// A work order that the planner agent of Insights proposed for a machine. A user applies it (a work order exists then)
/// or dismisses it. The ID is the ID of the agent action, so the proposal message is safe to repeat. Only a
/// <c>pending</c> draft changes.
/// </summary>
public class WorkOrderDraft : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    public WorkOrderDraft(
        Guid id,
        Guid companyId,
        Guid machineId,
        Guid caseId,
        Guid runId,
        string title,
        string? description,
        WorkOrderPriority priority,
        WorkOrderCategory category,
        int estimateMinutes
    )
    {
        Id = id;
        CompanyId = companyId;
        MachineId = machineId;
        CaseId = caseId;
        RunId = runId;
        Title = title;
        Description = description;
        Priority = priority;
        Category = category;
        EstimateMinutes = estimateMinutes;
        State = DraftState.Pending;
    }

    public Guid Id { get; private set; }

    public uint RowVersion { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid MachineId { get; private set; }

    /// <summary>The case of Insights that the draft belongs to.</summary>
    public Guid CaseId { get; private set; }

    public Guid RunId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public WorkOrderPriority Priority { get; private set; }

    public WorkOrderCategory Category { get; private set; }

    /// <summary>The estimate in minutes.</summary>
    public int EstimateMinutes { get; private set; }

    public DraftState State { get; private set; }

    public Guid? AppliedWorkOrderId { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Marks the draft as applied: the work order exists.
    /// </summary>
    /// <param name="workOrderId">The work order that the user created from the draft.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="WorkOrderDraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void MarkApplied(Guid workOrderId)
    {
        EnsurePending();
        AppliedWorkOrderId = workOrderId;
        State = DraftState.Applied;
    }

    /// <summary>
    /// Marks the draft as dismissed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="WorkOrderDraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void Dismiss()
    {
        EnsurePending();
        State = DraftState.Dismissed;
    }

    private void EnsurePending()
    {
        if (State != DraftState.Pending)
        {
            throw WorkOrderDraftInvariantErrors.NotPending(Id, State);
        }
    }
}
