using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;

/// <summary>
/// An operating phase that the ingest of a file proposed. Every value can be <see langword="null"/>: the user
/// completes it during the review. The draft follows its proposal entry as <see cref="MetricDefinitionDraft"/> does.
/// Its conditions and limit rows name definitions or drafts of definitions, so the user applies the phase draft only
/// after the drafts that it uses. The conditions and the limit rows are child entities with a cascade. A
/// removal keeps the row (soft delete).
/// </summary>
public class OperatingPhaseDraft : IDefinitionDraft, IAuditable, ISoftDeletable, IVersioned
{
    private readonly List<OperatingPhaseDraftCondition> _conditions = [];
    private readonly List<OperatingPhaseDraftLimit> _limits = [];

    private OperatingPhaseDraft()
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
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public int? Priority { get; private set; }
    public bool? IsTracked { get; private set; }
    public IReadOnlyList<OperatingPhaseDraftCondition> Conditions => _conditions;
    public IReadOnlyList<OperatingPhaseDraftLimit> Limits => _limits;
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Tells whether a condition or a limit row still waits for a draft of a definition.
    /// </summary>
    public bool HasUnresolvedTargets =>
        _conditions.Any(condition => condition.IsWaitingForDraft) || _limits.Any(limit => limit.IsWaitingForDraft);

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
    /// <param name="conditions">The proposed conditions.</param>
    /// <param name="limits">The proposed limit rows.</param>
    /// <returns>The new draft in the state <c>awaitingTarget</c>.</returns>
    public static OperatingPhaseDraft Propose(
        Guid id,
        Guid companyId,
        Guid proposalId,
        Guid? proposalEntryId,
        Guid? tentativeMachineId,
        PhaseDraftValues values,
        IReadOnlyList<PhaseDraftConditionValues> conditions,
        IReadOnlyList<PhaseDraftLimitValues> limits
    )
    {
        var draft = new OperatingPhaseDraft
        {
            Id = id,
            CompanyId = companyId,
            ProposalId = proposalId,
            ProposalEntryId = proposalEntryId,
            TargetMachineId = tentativeMachineId,
            SourceLocation = values.SourceLocation,
            Code = values.Code,
            Name = values.Name,
            Description = values.Description,
            Priority = values.Priority,
            IsTracked = values.IsTracked,
        };

        draft._conditions.AddRange(conditions.Select(OperatingPhaseDraftCondition.Create));
        draft._limits.AddRange(limits.Select(OperatingPhaseDraftLimit.Create));

        return draft;
    }

    /// <summary>
    /// Creates a draft for a machine that exists. The draft has no proposal entry and waits for a user at once.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the proposal.</param>
    /// <param name="proposalId">The proposal that holds the origin of the draft.</param>
    /// <param name="machineId">The machine of the draft.</param>
    /// <param name="values">The proposed values.</param>
    /// <param name="conditions">The proposed conditions.</param>
    /// <param name="limits">The proposed limit rows.</param>
    /// <returns>The new draft in the state <c>pending</c>.</returns>
    public static OperatingPhaseDraft ProposeOnMachine(
        Guid id,
        Guid companyId,
        Guid proposalId,
        Guid machineId,
        PhaseDraftValues values,
        IReadOnlyList<PhaseDraftConditionValues> conditions,
        IReadOnlyList<PhaseDraftLimitValues> limits
    )
    {
        var draft = Propose(id, companyId, proposalId, proposalEntryId: null, machineId, values, conditions, limits);
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
    /// Moves every condition and limit row that uses a metric draft to the definition that the draft became.
    /// </summary>
    /// <param name="draftId">The applied metric draft.</param>
    /// <param name="definitionId">The new metric definition.</param>
    /// <returns><see langword="true"/> when a row moved.</returns>
    public bool RetargetMetric(Guid draftId, Guid definitionId)
    {
        var movedConditions = _conditions.Count(condition => condition.RetargetMetric(draftId, definitionId));
        var movedLimits = _limits.Count(limit => limit.Retarget(draftId, definitionId));

        return movedConditions + movedLimits > 0;
    }

    /// <summary>
    /// Moves every condition that uses a status draft to the definition that the draft became.
    /// </summary>
    /// <param name="draftId">The applied status draft.</param>
    /// <param name="definitionId">The new status definition.</param>
    /// <returns><see langword="true"/> when a condition moved.</returns>
    public bool RetargetStatus(Guid draftId, Guid definitionId)
    {
        var movedConditions = _conditions.Count(condition => condition.RetargetStatus(draftId, definitionId));

        return movedConditions > 0;
    }

    /// <summary>
    /// Tells whether the phase draft uses a metric or a status draft.
    /// </summary>
    /// <param name="draftId">The metric or status draft.</param>
    /// <returns><see langword="true"/> when a condition or a limit row names the draft.</returns>
    public bool UsesDraft(Guid draftId) =>
        _conditions.Any(condition => condition.MetricDraftId == draftId || condition.StatusDraftId == draftId)
        || _limits.Any(limit => limit.MetricDraftId == draftId);

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
