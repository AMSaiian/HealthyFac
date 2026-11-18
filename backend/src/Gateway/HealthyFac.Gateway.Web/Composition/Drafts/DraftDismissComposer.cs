using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// Dismisses the open drafts of a machine or of an ingest attempt in Core and in Telemetry, and adds the counts.
/// </summary>
/// <param name="downstreamClient">Calls Core and Telemetry.</param>
public sealed class DraftDismissComposer(DownstreamClient downstreamClient)
{
    private const string DismissedCountProperty = "dismissedCount";

    private readonly DownstreamClient _downstreamClient = downstreamClient;

    /// <summary>
    /// Sends the dismiss to both services in parallel. The Gateway cannot make the two changes one unit of work, so a
    /// failure of one service after the other one changed leaves that change in place. A second request finishes the
    /// rest, because a dismiss changes only the drafts that are still open.
    /// </summary>
    /// <param name="corePath">The Core dismiss path.</param>
    /// <param name="telemetryPath">The Telemetry dismiss path.</param>
    /// <param name="incoming">The request of the person.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The sum of the dismissed drafts, or the one error that the combined calls give.</returns>
    public async Task<IResult> DismissAsync(
        string corePath,
        string telemetryPath,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        var results = await Task.WhenAll(
            _downstreamClient.SendAsync(
                ComposedClusters.Core, HttpMethod.Post, corePath, body: null, incoming, cancellationToken),
            _downstreamClient.SendAsync(
                ComposedClusters.Telemetry, HttpMethod.Post, telemetryPath, body: null, incoming, cancellationToken));

        return ComposedErrors.Combine(results) switch
        {
            ComposedOutcome.Failure failure => ComposedResults.Problem(failure.Status, failure.Problem),
            ComposedOutcome.Success success => Results.Json(
                new JsonObject { [DismissedCountProperty] = success.Bodies.Sum(ReadCount) }),
            _ => throw new InvalidOperationException(),
        };
    }

    private static int ReadCount(JsonNode body) =>
        body[DismissedCountProperty] is JsonValue value && value.TryGetValue<int>(out var count) ? count : 0;
}
