namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// Defines the exceptions that a definition draft throws when a caller breaks an invariant. The Application layer
/// checks the state first, so these exceptions mark a programming error.
/// </summary>
public static class DefinitionDraftInvariantErrors
{
    public static InvalidOperationException NotPending(Guid draftId, DefinitionDraftState state) =>
        new($"Draft '{draftId}' is {state.Value}, so it cannot be applied.");

    public static InvalidOperationException NotOpen(Guid draftId, DefinitionDraftState state) =>
        new($"Draft '{draftId}' is {state.Value}, so it cannot be dismissed.");
}
