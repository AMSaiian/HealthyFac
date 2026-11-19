using System.Text.Json;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// Gives the comparer of the sort values of each sort column of the draft grid. Core and Telemetry use the same
/// columns, and each column has one JSON kind in both.
/// </summary>
public static class DraftSortComparers
{
    public const string ProposedAt = "proposedAt";
    public const string State = "state";

    /// <summary>
    /// Finds the comparer of a sort column.
    /// </summary>
    /// <param name="sortColumn">The sort column of the request.</param>
    /// <returns>The comparer of the sort values of the column.</returns>
    /// <exception cref="CompositionException">Thrown when the column is not a draft grid column.</exception>
    public static IComparer<JsonElement> For(string sortColumn) =>
        sortColumn switch
        {
            ProposedAt => new JsonValueComparer<DateTime>(),
            State => new JsonValueComparer<int>(),
            _ => throw new CompositionException(CompositionErrors.SortColumnUnknown),
        };
}
