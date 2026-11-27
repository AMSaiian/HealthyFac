using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;

/// <summary>
/// A status definition that the ingest of a file proposed. Every value can be <see langword="null"/>. It follows its
/// proposal entry as <see cref="MetricDefinitionDraft"/> does. A removal keeps the row (soft delete).
/// </summary>
public class StatusDefinitionDraft : IDefinitionDraft, IAuditable, ISoftDeletable, IVersioned
{
    private StatusDefinitionDraft()
    {
        State = DefinitionDraftState.AwaitingTarget;
    }

    public Guid Id { get; private set; }
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ProposalId { get; private set; }
    public DraftProposal Proposal { get; private set; } = null!;
    public Guid? ProposalEntryId { get; private set; }
    public Guid? TargetMachineDraftId { get; private set; }
    public Guid? TargetMachineId { get; private set; }
    public DefinitionDraftState State { get; private set; }

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
    /// Creates a draft that waits for the machine check of its proposal entry.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="proposalId">The proposal that holds the origin of the draft.</param>
    /// <param name="proposalEntryId">The proposed machine that the draft belongs to, or <see langword="null"/>.</param>
    /// <param name="tentativeMachineId">
    /// The machine that the proposal named, or <see langword="null"/>. The machine check can still reject it.
    /// </param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft in the state <c>awaitingTarget</c>.</returns>
    public static StatusDefinitionDraft Propose(
        Guid id,
        Guid companyId,
        Guid proposalId,
        Guid? proposalEntryId,
        Guid? tentativeMachineId,
        StatusDraftValues values
    ) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            ProposalId = proposalId,
            ProposalEntryId = proposalEntryId,
            TargetMachineId = tentativeMachineId,
            SourceLocation = values.SourceLocation,
            Code = values.Code,
            Severity = values.Severity,
            Description = values.Description,
        };

    /// <summary>
    /// Creates a draft for a machine that exists. The draft has no proposal entry and waits for a user at once.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the proposal.</param>
    /// <param name="proposalId">The proposal that holds the origin of the draft.</param>
    /// <param name="machineId">The machine of the draft.</param>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new draft in the state <c>pending</c>.</returns>
    public static StatusDefinitionDraft ProposeOnMachine(
        Guid id,
        Guid companyId,
        Guid proposalId,
        Guid machineId,
        StatusDraftValues values
    )
    {
        var draft = Propose(id, companyId, proposalId, proposalEntryId: null, machineId, values);
        draft.State = DefinitionDraftState.Pending;

        return draft;
    }

    /// <inheritdoc/>
    public bool Follow(ProposalEntry entry, bool isTargetRegistrationDeleted)
    {
        var outcome = DefinitionDraftFollowing.Resolve(
            new DefinitionDraftFollowInput
            {
                TargetMachineId = TargetMachineId,
                TargetMachineDraftId = TargetMachineDraftId,
                State = State,
            },
            entry,
            isTargetRegistrationDeleted);

        TargetMachineId = outcome.TargetMachineId;
        TargetMachineDraftId = outcome.TargetMachineDraftId;
        State = outcome.State;

        return outcome.IsRemovalRequired;
    }

    /// <summary>
    /// Marks the draft as applied.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DefinitionDraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void MarkApplied()
    {
        if (State != DefinitionDraftState.Pending)
        {
            throw DefinitionDraftInvariantErrors.NotPending(Id, State);
        }

        State = DefinitionDraftState.Applied;
    }

    /// <summary>
    /// Marks the draft as dismissed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DefinitionDraftInvariantErrors"/> when the draft is neither pending nor awaiting its
    /// target.
    /// </exception>
    public void Dismiss()
    {
        if (State != DefinitionDraftState.Pending && State != DefinitionDraftState.AwaitingTarget)
        {
            throw DefinitionDraftInvariantErrors.NotOpen(Id, State);
        }

        State = DefinitionDraftState.Dismissed;
    }
}
