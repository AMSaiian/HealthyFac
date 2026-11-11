namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// Defines the exceptions that a draft throws when a caller breaks an invariant. The Application layer checks the
/// state first, so these exceptions mark a programming error.
/// </summary>
public static class DraftInvariantErrors
{
    public static InvalidOperationException NotPending(Guid draftId, DraftState state) =>
        new($"Draft '{draftId}' is {state.Value}, so it cannot change.");

    public static InvalidOperationException NotTargetedAtMachineDraft(Guid draftId) =>
        new($"Draft '{draftId}' has no pending machine draft as target, so it cannot move to a machine.");
}
