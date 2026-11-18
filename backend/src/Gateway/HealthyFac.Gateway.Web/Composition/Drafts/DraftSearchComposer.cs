using System.Text.Json;
using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>The scope of a composed draft search: one machine, or one ingest attempt.</summary>
public sealed record DraftSearchScope
{
    /// <summary>The API version segment of the request (for example <c>v1</c>).</summary>
    public required string Version { get; init; }

    /// <summary>The encrypted machine ID of the route, or <see langword="null"/>.</summary>
    public string? MachineId { get; init; }

    /// <summary>The encrypted ingest attempt ID of the route, or <see langword="null"/>.</summary>
    public string? AttemptId { get; init; }

    public string CorePath => MachineId is not null
        ? DraftCompositionRoutes.CoreMachineSearch(Version, MachineId)
        : DraftCompositionRoutes.CoreAttemptSearch(Version, AttemptId!);

    public string TelemetryPath => MachineId is not null
        ? DraftCompositionRoutes.TelemetryMachineSearch(Version, MachineId)
        : DraftCompositionRoutes.TelemetryAttemptSearch(Version, AttemptId!);
}

/// <summary>
/// Composes one page of the draft grid from Core (machine drafts) and Telemetry (metric and status drafts). Both
/// services page by the same sort key, so the Gateway reads one page from each in parallel, merges them, and keeps one
/// position for each source. A source that has no more rows is still called with a limit of 1 and its last position, so
/// its total count stays in the sum on every page; its rows are not used.
/// </summary>
/// <param name="downstreamClient">Calls Core and Telemetry.</param>
/// <param name="machineNameFiller">Gives the Telemetry rows their machine name.</param>
public sealed class DraftSearchComposer(DownstreamClient downstreamClient, MachineNameFiller machineNameFiller)
{
    private const int FinishedSourceLimit = 1;
    private const string LimitProperty = "limit";
    private const string SortProperty = "sort";
    private const string FilterProperty = "filter";
    private const string AfterProperty = "after";
    private const string ItemsProperty = "items";
    private const string TotalCountProperty = "totalCount";
    private const string NextPositionProperty = "nextPosition";

    private readonly DownstreamClient _downstreamClient = downstreamClient;
    private readonly MachineNameFiller _machineNameFiller = machineNameFiller;

    /// <summary>
    /// Runs one composed search.
    /// </summary>
    /// <param name="scope">The machine or the ingest attempt of the route.</param>
    /// <param name="body">The JSON body of the request.</param>
    /// <param name="incoming">The request of the person.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The page, or the one error that the combined calls give.</returns>
    public async Task<IResult> SearchAsync(
        DraftSearchScope scope,
        JsonObject? body,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        var request = DraftSearchRequest.TryRead(body, out var errors);

        if (request is null)
        {
            return ComposedResults.Validation(errors);
        }

        var split = DraftFilterSplitter.Split(request.Filter);
        var isCoreCalled = !split.Core.IsSkipped;
        var isTelemetryCalled = !split.Telemetry.IsSkipped;
        var isCoreFinished = request.After?.IsCoreFinished == true;
        var isTelemetryFinished = request.After?.IsTelemetryFinished == true;
        var calls = new List<Task<DownstreamResult>>();

        if (isCoreCalled)
        {
            calls.Add(
                Call(
                    ComposedClusters.Core,
                    scope.CorePath,
                    request,
                    split.Core,
                    request.After?.Core,
                    isCoreFinished,
                    incoming,
                    cancellationToken));
        }

        if (isTelemetryCalled)
        {
            calls.Add(
                Call(
                    ComposedClusters.Telemetry,
                    scope.TelemetryPath,
                    request,
                    split.Telemetry,
                    request.After?.Telemetry,
                    isTelemetryFinished,
                    incoming,
                    cancellationToken));
        }

        var results = await Task.WhenAll(calls);
        var outcome = ComposedErrors.Combine(results);

        if (outcome is ComposedOutcome.Failure failure)
        {
            return ComposedResults.Problem(failure.Status, failure.Problem);
        }

        var bodies = ((ComposedOutcome.Success)outcome).Bodies;
        var coreBody = isCoreCalled ? bodies[index: 0] : null;
        var telemetryBody = isTelemetryCalled ? bodies[isCoreCalled ? 1 : 0] : null;
        var page = DraftPageMerger.Merge(
            isCoreFinished ? [] : ReadRows(coreBody),
            isTelemetryFinished ? [] : ReadRows(telemetryBody),
            request.SortColumn,
            request.IsDescending,
            request.Limit,
            request.After);

        var nameFailure = await _machineNameFiller.FillAsync(page.Items, scope.Version, incoming, cancellationToken);

        return nameFailure is not null
            ? ComposedResults.Problem(nameFailure.Status, nameFailure.Problem)
            : Results.Json(
                new JsonObject
                {
                    [ItemsProperty] = new JsonArray([.. page.Items]),
                    [TotalCountProperty] = ReadTotalCount(coreBody) + ReadTotalCount(telemetryBody),
                    [NextPositionProperty] = JsonSerializer.SerializeToNode(
                        page.NextPosition,
                        JsonSerializerOptions.Web),
                });
    }

    private Task<DownstreamResult> Call(
        string clusterId,
        string path,
        DraftSearchRequest request,
        SourceFilter filter,
        KeysetPositionDto? after,
        bool isFinished,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        var body = new JsonObject
        {
            [LimitProperty] = isFinished ? FinishedSourceLimit : request.Limit,
            [SortProperty] = request.SortBody,
        };

        if (filter.Filter is not null)
        {
            body[FilterProperty] = filter.Filter.DeepClone();
        }

        if (after is not null)
        {
            body[AfterProperty] = JsonSerializer.SerializeToNode(after, JsonSerializerOptions.Web);
        }

        return _downstreamClient.SendAsync(clusterId, HttpMethod.Post, path, body, incoming, cancellationToken);
    }

    private static List<JsonObject> ReadRows(JsonNode? body) =>
        [.. (body?[ItemsProperty] as JsonArray ?? []).OfType<JsonObject>()];

    private static int ReadTotalCount(JsonNode? body) =>
        body?[TotalCountProperty] is JsonValue value && value.TryGetValue<int>(out var count) ? count : 0;
}
