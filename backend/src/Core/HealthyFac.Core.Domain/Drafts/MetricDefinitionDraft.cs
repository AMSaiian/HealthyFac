using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// A metric definition that the ingest of a file proposed. Every value can be <see langword="null"/>: the user
/// completes it during the review. The draft has exactly one target: a machine, or the pending create draft of a
/// machine. The apply of that machine draft moves the child to the new machine
/// (<see cref="RetargetToMachine"/>). A removal keeps the row (soft delete).
/// </summary>
public class MetricDefinitionDraft : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private MetricDefinitionDraft()
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

    public string? Key { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>One of <c>numeric</c>, <c>boolean</c>, <c>stateSet</c>, or <see langword="null"/>.</summary>
    public string? ValueKind { get; private set; }

    public string? QuantityKindName { get; private set; }
    public string? UnitName { get; private set; }
    public double? Min { get; private set; }
    public double? Max { get; private set; }

    /// <summary>The proposed state codes of a state set, or <see langword="null"/>.</summary>
    public IReadOnlyList<string>? States { get; private set; }

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
    /// <param name="targetMachineId">The machine that the metric belongs to.</param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft.</returns>
    public static MetricDefinitionDraft ForMachine(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        Guid targetMachineId,
        MetricDraftValues values
    ) =>
        Create(id, companyId, attemptId, fileId, values, targetMachineId, targetMachineDraftId: null);

    /// <summary>
    /// Creates a pending draft for the machine that a pending create draft proposes.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="attemptId">The ingest attempt that proposed the draft.</param>
    /// <param name="fileId">The file that the draft came from.</param>
    /// <param name="targetMachineDraftId">The create draft of the machine that the metric belongs to.</param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft.</returns>
    public static MetricDefinitionDraft ForMachineDraft(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        Guid targetMachineDraftId,
        MetricDraftValues values
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

    private static MetricDefinitionDraft Create(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        MetricDraftValues values,
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
            Key = values.Key,
            Name = values.Name,
            Description = values.Description,
            ValueKind = values.ValueKind,
            QuantityKindName = values.QuantityKindName,
            UnitName = values.UnitName,
            Min = values.Min,
            Max = values.Max,
            States = values.States is null ? null : [.. values.States],
        };

    private void EnsurePending()
    {
        if (State != DraftState.Pending)
        {
            throw DraftInvariantErrors.NotPending(Id, State);
        }
    }
}
