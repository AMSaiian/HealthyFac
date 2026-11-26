using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

/// <summary>One row of a stubbed draft source: its ID, kind, state rank, and extra members of the row.</summary>
public sealed record StubDraftRow(string Id, string Kind, int StateRank, JsonObject Extra);

/// <summary>
/// Makes a <see cref="StubApiServer"/> answer a draft search like a service does: it sorts by the state rank and then
/// the ID, reads <c>limit</c> and <c>after</c> from the body, and returns <c>items</c>, <c>totalCount</c>, and
/// <c>nextPosition</c>. It ignores the filter, so a test checks the filter in the recorded request.
/// </summary>
public static class DraftSourceStub
{
    private const string SortValueProperty = "sortValue";
    private const string IdProperty = "id";
    private const string PositionProperty = "position";

    /// <summary>
    /// Serves a draft search path from rows.
    /// </summary>
    /// <param name="server">The stub server.</param>
    /// <param name="path">The path that the service receives.</param>
    /// <param name="rows">The rows of the source.</param>
    public static void Serve(StubApiServer server, string path, IReadOnlyList<StubDraftRow> rows) =>
        server.Respond(HttpMethod.Post, path, requestBody => (StatusCodes.Status200OK, Answer(requestBody, rows)));

    private static string Answer(string requestBody, IReadOnlyList<StubDraftRow> rows)
    {
        var request = JsonNode.Parse(requestBody)!.AsObject();
        var limit = request["limit"]!.GetValue<int>();
        var after = request["after"] as JsonObject;
        var afterRank = after?[SortValueProperty]?.GetValue<int>();
        var afterId = after?[IdProperty]?.GetValue<string>();

        var remaining = rows
            .OrderBy(row => row.StateRank)
            .ThenBy(row => row.Id, StringComparer.Ordinal)
            .Where(row => afterRank is null
                || row.StateRank > afterRank
                || (row.StateRank == afterRank && string.CompareOrdinal(row.Id, afterId) > 0))
            .ToList();

        var page = remaining.Take(limit).Select(ToItem).ToList();
        var hasMore = remaining.Count > page.Count;

        return new JsonObject
        {
            ["items"] = new JsonArray([.. page]),
            ["totalCount"] = rows.Count,
            ["nextPosition"] = hasMore ? page[^1][PositionProperty]!.DeepClone() : null,
        }.ToJsonString();
    }

    private static JsonObject ToItem(StubDraftRow row)
    {
        var item = (JsonObject)row.Extra.DeepClone();
        item[IdProperty] = row.Id;
        item["kind"] = row.Kind;
        item[PositionProperty] = new JsonObject { [SortValueProperty] = row.StateRank, [IdProperty] = row.Id };

        return item;
    }
}
