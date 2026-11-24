using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;

/// <summary>
/// A metric definition that the ingest of a file proposed. Every value can be <see langword="null"/>: the user
/// completes it during the review. The draft waits in the state <c>awaitingTarget</c> until the machine check of its
/// proposal entry tells the target: a machine, or the pending create draft of a machine. A removal keeps the row
/// (soft delete).
/// </summary>
public class MetricDefinitionDraft : IDefinitionDraft, IAuditable, ISoftDeletable, IVersioned
{
    private MetricDefinitionDraft()
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

    public double? WarningLow { get; private set; }
    public double? WarningHigh { get; private set; }
    public double? AlarmLow { get; private set; }
    public double? AlarmHigh { get; private set; }

    /// <summary>One of <c>level</c>, <c>counter</c>, or <see langword="null"/>. Missing means <c>level</c>.</summary>
    public string? Behavior { get; private set; }

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
    public static MetricDefinitionDraft Propose(
        Guid id,
        Guid companyId,
        Guid proposalId,
        Guid? proposalEntryId,
        Guid? tentativeMachineId,
        MetricDraftValues values
    ) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            ProposalId = proposalId,
            ProposalEntryId = proposalEntryId,
            TargetMachineId = tentativeMachineId,
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
            WarningLow = values.WarningLow,
            WarningHigh = values.WarningHigh,
            AlarmLow = values.AlarmLow,
            AlarmHigh = values.AlarmHigh,
            Behavior = values.Behavior,
        };

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
