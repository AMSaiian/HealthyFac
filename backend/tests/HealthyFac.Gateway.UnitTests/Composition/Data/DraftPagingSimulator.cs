using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.Web.Composition.Drafts;

namespace HealthyFac.Gateway.UnitTests.Composition.Data;

/// <summary>
/// Stands in for the keyset paging of one service: it sorts its rows by the sort value and then the ID, and it returns
/// the rows after a position, like <c>ToKeysetResponseAsync</c>. Tests read a merged grid page by page through it.
/// </summary>
public sealed class DraftPagingSimulator(IReadOnlyList<(string SortValueJson, string Id)> rows)
{
    private readonly IReadOnlyList<(string SortValueJson, string Id)> _rows = rows;

    /// <summary>
    /// Reads rows from a source and from the other source through <see cref="DraftPageMerger"/>, page by page, until
    /// no next position remains.
    /// </summary>
    /// <param name="core">The Core source.</param>
    /// <param name="telemetry">The Telemetry source.</param>
    /// <param name="sortColumn">The sort column.</param>
    /// <param name="isDescending">Whether the direction is descending.</param>
    /// <param name="limit">The page size.</param>
    /// <returns>The IDs of every row, in the order of the pages.</returns>
    public static IReadOnlyList<string> ReadAll(
        DraftPagingSimulator core,
        DraftPagingSimulator telemetry,
        string sortColumn,
        bool isDescending,
        int limit
    )
    {
        var ids = new List<string>();
        ComposedPosition? position = null;
        var pageCount = 0;

        do
        {
            var coreRows = position?.IsCoreFinished == true
                ? []
                : core.Read(position?.Core, sortColumn, isDescending, limit);
            var telemetryRows = position?.IsTelemetryFinished == true
                ? []
                : telemetry.Read(position?.Telemetry, sortColumn, isDescending, limit);
            var page = DraftPageMerger.Merge(coreRows, telemetryRows, sortColumn, isDescending, limit, position);

            ids.AddRange(page.Items.Select(item => item[propertyName: "id"]!.GetValue<string>()));
            position = page.NextPosition;
            pageCount++;
        }
        while (position is not null && pageCount < MaxPages);

        return ids;
    }

    /// <summary>The page limit of the loop, so a faulty merge fails the test instead of looping.</summary>
    public const int MaxPages = 50;

    private IReadOnlyList<JsonObject> Read(
        KeysetPositionDto? after,
        string sortColumn,
        bool isDescending,
        int limit
    )
    {
        var comparer = DraftSortComparers.For(sortColumn);
        var sign = isDescending ? -1 : 1;
        var parsed = _rows
            .Select(row => (Value: JsonDocument.Parse(row.SortValueJson).RootElement.Clone(), row.Id))
            .OrderBy(row => row.Value, new SignedComparer(comparer, sign))
            .ThenBy(row => row.Id, StringComparer.Ordinal);

        return
        [
            .. parsed
                .Where(row => after is null
                    || comparer.Compare(row.Value, after.SortValue) * sign > 0
                    || (comparer.Compare(row.Value, after.SortValue) == 0
                        && string.CompareOrdinal(row.Id, after.Id) > 0))
                .Take(limit)
                .Select(row => new JsonObject
                {
                    [propertyName: "id"] = row.Id,
                    [propertyName: "position"] = new JsonObject
                    {
                        [propertyName: "sortValue"] = JsonNode.Parse(row.Value.GetRawText()),
                        [propertyName: "id"] = row.Id,
                    },
                }),
        ];
    }

    private sealed class SignedComparer(IComparer<JsonElement> inner, int sign) : IComparer<JsonElement>
    {
        public int Compare(JsonElement x, JsonElement y) => inner.Compare(x, y) * sign;
    }
}
