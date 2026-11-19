using System.Text.Json;
using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts.Search;

/// <summary>One merged page: the rows in order and the position that reads the next page.</summary>
public sealed record MergedDraftPage
{
    public required IReadOnlyList<JsonObject> Items { get; init; }

    /// <summary>The position of the next page, or <see langword="null"/> when no source has more rows.</summary>
    public ComposedPosition? NextPosition { get; init; }
}

/// <summary>
/// Merges one page of Core rows and one page of Telemetry rows into one page in the sort order of the request. Each
/// source sorts and pages by the same key (the sort value, then the ID), so the merge needs only the sort value of
/// each row.
/// </summary>
public static class DraftPageMerger
{
    private const string PositionProperty = "position";
    private const int CoreOrder = 0;
    private const int TelemetryOrder = 1;

    /// <summary>
    /// Merges the rows of the two sources. Equal sort values put a Core row before a Telemetry row, then keep the order
    /// of the source, so the merge is stable. Each source sends up to <paramref name="limit"/> rows, so the merged page
    /// is complete whichever source supplies the rows.
    /// </summary>
    /// <param name="coreRows">The Core rows of the page, empty when Core was not called.</param>
    /// <param name="telemetryRows">The Telemetry rows of the page, empty when Telemetry was not called.</param>
    /// <param name="sortColumn">The sort column of the request (<see cref="DraftSortComparers"/>).</param>
    /// <param name="isDescending">Whether the sort direction is descending.</param>
    /// <param name="limit">The page size of the request.</param>
    /// <param name="previous">The position of the request, or <see langword="null"/> for the first page.</param>
    /// <returns>The merged page and the position of the next page.</returns>
    /// <exception cref="CompositionException">Thrown when a row has no position or a wrong sort value.</exception>
    public static MergedDraftPage Merge(
        IReadOnlyList<JsonObject> coreRows,
        IReadOnlyList<JsonObject> telemetryRows,
        string sortColumn,
        bool isDescending,
        int limit,
        ComposedPosition? previous
    )
    {
        var comparer = DraftSortComparers.For(sortColumn);
        var rows = coreRows
            .Select((row, index) => ToMergeRow(row, CoreOrder, index, comparer))
            .Concat(telemetryRows.Select((row, index) => ToMergeRow(row, TelemetryOrder, index, comparer)))
            .ToList();

        var ordered = isDescending
            ? rows.OrderByDescending(row => row.Position.SortValue, comparer)
            : rows.OrderBy(row => row.Position.SortValue, comparer);
        var taken = ordered.ThenBy(row => row.Source).ThenBy(row => row.Index).Take(limit).ToList();

        var takenCore = taken.Where(row => row.Source == CoreOrder).ToList();
        var takenTelemetry = taken.Where(row => row.Source == TelemetryOrder).ToList();
        var isCoreFinished = IsFinished(coreRows.Count, takenCore.Count, limit);
        var isTelemetryFinished = IsFinished(telemetryRows.Count, takenTelemetry.Count, limit);

        return new MergedDraftPage
        {
            Items = [.. taken.Select(row => (JsonObject)row.Row.DeepClone())],
            NextPosition = isCoreFinished && isTelemetryFinished
                ? null
                : new ComposedPosition
                {
                    Core = takenCore.LastOrDefault()?.Position ?? previous?.Core,
                    Telemetry = takenTelemetry.LastOrDefault()?.Position ?? previous?.Telemetry,
                    IsCoreFinished = isCoreFinished,
                    IsTelemetryFinished = isTelemetryFinished,
                },
        };
    }

    private static bool IsFinished(int returnedCount, int takenCount, int limit) =>
        returnedCount < limit && takenCount == returnedCount;

    private static MergeRow ToMergeRow(JsonObject row, int source, int index, IComparer<JsonElement> comparer)
    {
        var position = row[PositionProperty]?.Deserialize<KeysetPositionDto>(JsonSerializerOptions.Web)
            ?? throw new CompositionException(CompositionErrors.RowPositionMissing);

        // The sort of the rows would wrap an exception of the comparer, so a value of the wrong kind fails here first.
        comparer.Compare(position.SortValue, position.SortValue);

        return new MergeRow { Row = row, Position = position, Source = source, Index = index };
    }

    private sealed record MergeRow
    {
        public required JsonObject Row { get; init; }

        public required KeysetPositionDto Position { get; init; }

        public required int Source { get; init; }

        public required int Index { get; init; }
    }
}
