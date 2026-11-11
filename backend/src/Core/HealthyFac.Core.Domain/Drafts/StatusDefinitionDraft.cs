using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// A status definition that the ingest of a file proposed. Every value can be <see langword="null"/>. The draft has
/// exactly one target, as <see cref="MetricDefinitionDraft"/> has. A removal keeps the row (soft delete).
/// </summary>
public class StatusDefinitionDraft : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private StatusDefinitionDraft()
    {
        State = DraftState.Pending;
    }

    public Guid Id { get; private set; }
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid AttemptId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid? TargetMachineDraftId { get; private set; }
    public Guid? TargetMachineId { get; private set; }
    public DraftState State { get; private set; }

    /// <summary>The place in the document where the values were found, for the review.</summary>
    public string? SourceLocation { get; private set; }

    public string? Code { get; private set; }

    /// <summary>One of <c>info</c>, <c>warning</c>, <c>error</c>, or <see langword="null"/>.</summary>
    public string? Severity { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Creates a pending draft for an existing machine.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="attemptId">The ingest attempt that proposed the draft.</param>
    /// <param name="fileId">The file that the draft came from.</param>
    /// <param name="targetMachineId">The machine that the status belongs to.</param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft.</returns>
    public static StatusDefinitionDraft ForMachine(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        Guid targetMachineId,
        StatusDraftValues values
    ) =>
        Create(id, companyId, attemptId, fileId, values, targetMachineId, targetMachineDraftId: null);

    /// <summary>
    /// Creates a pending draft for the machine that a pending create draft proposes.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="attemptId">The ingest attempt that proposed the draft.</param>
    /// <param name="fileId">The file that the draft came from.</param>
    /// <param name="targetMachineDraftId">The create draft of the machine that the status belongs to.</param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft.</returns>
    public static StatusDefinitionDraft ForMachineDraft(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        Guid targetMachineDraftId,
        StatusDraftValues values
    ) =>
        Create(id, companyId, attemptId, fileId, values, targetMachineId: null, targetMachineDraftId);

    /// <summary>
    /// Moves the draft from the create draft of a machine to the machine that the apply created.
    /// </summary>
    /// <param name="machineId">The created machine.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftInvariantErrors"/> when the draft has no machine draft as target or is not pending.
    /// </exception>
    public void RetargetToMachine(Guid machineId)
    {
        if (TargetMachineDraftId is null || State != DraftState.Pending)
        {
            throw DraftInvariantErrors.NotTargetedAtMachineDraft(Id);
        }

        TargetMachineId = machineId;
        TargetMachineDraftId = null;
    }

    /// <summary>
    /// Marks the draft as applied.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void MarkApplied()
    {
        EnsurePending();
        State = DraftState.Applied;
    }

    /// <summary>
    /// Marks the draft as dismissed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void Dismiss()
    {
        EnsurePending();
        State = DraftState.Dismissed;
    }

    private static StatusDefinitionDraft Create(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        StatusDraftValues values,
        Guid? targetMachineId,
        Guid? targetMachineDraftId
    ) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            AttemptId = attemptId,
            FileId = fileId,
            TargetMachineId = targetMachineId,
            TargetMachineDraftId = targetMachineDraftId,
            SourceLocation = values.SourceLocation,
            Code = values.Code,
            Severity = values.Severity,
            Description = values.Description,
        };

    private void EnsurePending()
    {
        if (State != DraftState.Pending)
        {
            throw DraftInvariantErrors.NotPending(Id, State);
        }
    }
}
