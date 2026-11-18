using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Machines;

/// <summary>
/// Composes the machine grid: Core answers the page, and Telemetry adds its pending metric and status draft counts to
/// the rows. The filter <c>hasPendingDrafts</c> needs the Telemetry counts before Core runs the query.
/// </summary>
/// <param name="downstreamClient">Calls Core and Telemetry.</param>
public sealed class MachineSearchComposer(DownstreamClient downstreamClient)
{
    private const string FilterProperty = "filter";
    private const string HasPendingDraftsProperty = "hasPendingDrafts";
    private const string PendingMachineIdsProperty = "pendingDefinitionDraftMachineIds";
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string PendingDraftCountProperty = "pendingDraftCount";
    private const string CountsProperty = "counts";
    private const string MachineIdProperty = "machineId";
    private const string CountProperty = "count";

    private readonly DownstreamClient _downstreamClient = downstreamClient;

    /// <summary>
    /// Runs the machine search. The Gateway sets <c>filter.pendingDefinitionDraftMachineIds</c> itself, so it removes a
    /// value that the client sent.
    /// </summary>
    /// <param name="version">The API version of the request.</param>
    /// <param name="body">The JSON body of the request.</param>
    /// <param name="incoming">The request of the person.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The Core page with the added counts, or the one error that the combined calls give.</returns>
    public async Task<IResult> SearchAsync(
        string version,
        JsonObject? body,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        if (body is null)
        {
            return ComposedResults.Validation(
                new Dictionary<string, string[]> { [string.Empty] = [CompositionErrors.BodyInvalid] });
        }

        var coreBody = (JsonObject)body.DeepClone();
        var filter = coreBody[FilterProperty] as JsonObject;
        filter?.Remove(PendingMachineIdsProperty);
        var isFilteredByDrafts = filter?[HasPendingDraftsProperty] is JsonValue flag && flag.TryGetValue<bool>(out _);
        var countsTask = _downstreamClient.SendAsync(
            ComposedClusters.Telemetry,
            HttpMethod.Get,
            MachineCompositionRoutes.TelemetryPendingCounts(version),
            body: null,
            incoming,
            cancellationToken);

        DownstreamResult[] results;

        if (isFilteredByDrafts)
        {
            var counts = await countsTask;

            if (ComposedErrors.Combine([counts]) is ComposedOutcome.Failure countsFailure)
            {
                return ComposedResults.Problem(countsFailure.Status, countsFailure.Problem);
            }

            filter![PendingMachineIdsProperty] = new JsonArray(
                [.. ReadCounts(counts).Select(count => JsonValue.Create(count.MachineId)!)]);
            results = [await CallCoreAsync(version, coreBody, incoming, cancellationToken), counts];
        }
        else
        {
            results = await Task.WhenAll(CallCoreAsync(version, coreBody, incoming, cancellationToken), countsTask);
        }

        return ComposedErrors.Combine(results) switch
        {
            ComposedOutcome.Failure failure => ComposedResults.Problem(failure.Status, failure.Problem),
            ComposedOutcome.Success success =>
                Results.Json(AddCounts((JsonObject)success.Bodies[index: 0], results[1])),
            _ => throw new InvalidOperationException(),
        };
    }

    private Task<DownstreamResult> CallCoreAsync(
        string version,
        JsonObject coreBody,
        HttpContext incoming,
        CancellationToken cancellationToken
    ) =>
        _downstreamClient.SendAsync(
            ComposedClusters.Core,
            HttpMethod.Post,
            MachineCompositionRoutes.CoreMachineSearch(version),
            coreBody,
            incoming,
            cancellationToken);

    private static JsonObject AddCounts(JsonObject page, DownstreamResult countsResult)
    {
        var counts = ReadCounts(countsResult).ToDictionary(count => count.MachineId, count => count.Count);

        foreach (var row in (page[ItemsProperty] as JsonArray ?? []).OfType<JsonObject>())
        {
            var id = row[IdProperty] is JsonValue idValue && idValue.TryGetValue<string>(out var text) ? text : null;
            var current = row[PendingDraftCountProperty] is JsonValue own && own.TryGetValue<int>(out var n) ? n : 0;

            if (id is not null && counts.TryGetValue(id, out var added))
            {
                row[PendingDraftCountProperty] = current + added;
            }
        }

        return page;
    }

    private static IEnumerable<(string MachineId, int Count)> ReadCounts(DownstreamResult result) =>
        result is DownstreamResult.Success success
            ? (success.Body[CountsProperty] as JsonArray ?? [])
            .OfType<JsonObject>()
            .Where(item => item[MachineIdProperty] is JsonValue && item[CountProperty] is JsonValue)
            .Select(item => (item[MachineIdProperty]!.GetValue<string>(), item[CountProperty]!.GetValue<int>()))
            : [];
}
