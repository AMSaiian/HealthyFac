namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// Defines the exceptions that a draft throws when a caller breaks an invariant. The Application layer checks the
/// state first, so these exceptions mark a programming error.
/// </summary>
public static class DraftInvariantErrors
{
    public static InvalidOperationException NotPending(Guid draftId, DraftState state) =>
        new($"Draft '{draftId}' is {state.Value}, so it cannot change.");
}
