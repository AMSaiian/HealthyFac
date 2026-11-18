using System.Linq.Expressions;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// Defines the open definition drafts for queries: a draft that waits for its target or for a user. The one
/// definition is an expression, because EF Core cannot translate a call to a C# method inside a query.
/// </summary>
public static class OpenDefinitionDrafts
{
    /// <summary>
    /// Builds the predicate "the draft is <c>awaitingTarget</c> or <c>pending</c>".
    /// </summary>
    /// <typeparam name="TDraft">The metric or status draft type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public static Expression<Func<TDraft, bool>> Predicate<TDraft>()
        where TDraft : class, IDefinitionDraft =>
        draft => draft.State == DefinitionDraftState.AwaitingTarget || draft.State == DefinitionDraftState.Pending;
}
