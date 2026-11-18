namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// The one mapping from the state of a proposal entry to the target and the state of a draft. The metric draft and
/// the status draft both apply its result.
/// </summary>
public static class DefinitionDraftFollowing
{
    /// <param name="draft">The current target and state of the draft.</param>
    /// <param name="entry">The proposal entry of the draft.</param>
    /// <param name="isTargetRegistrationDeleted">
    /// Whether the machine of an <c>onMachine</c> entry has a soft-deleted registration.
    /// </param>
    /// <returns>The new target and state, and whether the caller must remove the draft.</returns>
    public static DefinitionDraftFollowOutcome Resolve(
        DefinitionDraftFollowInput draft,
        ProposalEntry entry,
        bool isTargetRegistrationDeleted
    )
    {
        var unchanged = new DefinitionDraftFollowOutcome
        {
            TargetMachineId = draft.TargetMachineId,
            TargetMachineDraftId = draft.TargetMachineDraftId,
            State = draft.State,
        };

        if (draft.State != DefinitionDraftState.AwaitingTarget && draft.State != DefinitionDraftState.Pending)
        {
            return unchanged;
        }

        if (entry.State == ProposalEntryState.Rejected)
        {
            return unchanged with { IsRemovalRequired = draft.State == DefinitionDraftState.AwaitingTarget };
        }

        if (entry.State == ProposalEntryState.OnMachineDraft)
        {
            return new DefinitionDraftFollowOutcome
            {
                TargetMachineId = null,
                TargetMachineDraftId = entry.Id,
                State = DefinitionDraftState.Pending,
            };
        }

        if (entry.State == ProposalEntryState.OnMachine)
        {
            return new DefinitionDraftFollowOutcome
            {
                TargetMachineId = entry.MachineId,
                TargetMachineDraftId = null,
                State = isTargetRegistrationDeleted ? DefinitionDraftState.Dismissed : DefinitionDraftState.Pending,
            };
        }

        var isMachineDraftDismissedWithoutMachine =
            entry.State == ProposalEntryState.MachineDraftDismissed && draft.TargetMachineId is null;

        return isMachineDraftDismissedWithoutMachine
            ? unchanged with { State = DefinitionDraftState.Dismissed }
            : unchanged;
    }
}

/// <summary>The current target and state of a draft.</summary>
public sealed record DefinitionDraftFollowInput
{
    public Guid? TargetMachineId { get; init; }

    public Guid? TargetMachineDraftId { get; init; }

    public required DefinitionDraftState State { get; init; }
}

/// <summary>The target and state that a draft takes, and whether the caller must remove it.</summary>
public sealed record DefinitionDraftFollowOutcome
{
    public Guid? TargetMachineId { get; init; }

    public Guid? TargetMachineDraftId { get; init; }

    public required DefinitionDraftState State { get; init; }

    public bool IsRemovalRequired { get; init; }
}
