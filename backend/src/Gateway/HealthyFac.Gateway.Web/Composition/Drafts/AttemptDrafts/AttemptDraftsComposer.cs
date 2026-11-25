using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts.AttemptDrafts;

/// <summary>
/// Composes the view of one ingest attempt or one chat: the machine drafts of Core and the metric, status, and phase
/// drafts of Telemetry, in one group for each machine.
/// </summary>
/// <param name="downstreamClient">Calls Core and Telemetry.</param>
/// <param name="machineNameFiller">Reads the names of the groups that Core did not build.</param>
public sealed class AttemptDraftsComposer(DownstreamClient downstreamClient, MachineNameFiller machineNameFiller)
{
    private const string GroupsProperty = "groups";
    private const string MetricsProperty = "metrics";
    private const string StatusesProperty = "statuses";
    private const string PhasesProperty = "phases";
    private const string MachineNameProperty = "machineName";

    private readonly DownstreamClient _downstreamClient = downstreamClient;
    private readonly MachineNameFiller _machineNameFiller = machineNameFiller;

    /// <summary>
    /// Reads and groups the drafts of an attempt or a chat.
    /// </summary>
    /// <param name="version">The API version of the request.</param>
    /// <param name="corePath">The Core path of the grouped machine drafts of the scope.</param>
    /// <param name="telemetryPath">The Telemetry path of the definition drafts of the scope.</param>
    /// <param name="incoming">The request of the person.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The groups, or the one error that the combined calls give.</returns>
    public async Task<IResult> GetAsync(
        string version,
        string corePath,
        string telemetryPath,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        var results = await Task.WhenAll(
            _downstreamClient.SendAsync(
                ComposedClusters.Core,
                HttpMethod.Get,
                corePath,
                body: null,
                incoming,
                cancellationToken),
            _downstreamClient.SendAsync(
                ComposedClusters.Telemetry,
                HttpMethod.Get,
                telemetryPath,
                body: null,
                incoming,
                cancellationToken));

        if (ComposedErrors.Combine(results) is ComposedOutcome.Failure failure)
        {
            return ComposedResults.Problem(failure.Status, failure.Problem);
        }

        var groups = AttemptDraftGrouper.Group(
            ReadObjects(results[0], GroupsProperty),
            ReadObjects(results[1], MetricsProperty),
            ReadObjects(results[1], StatusesProperty),
            ReadObjects(results[1], PhasesProperty));

        var unnamed = groups
            .Where(group => group.LookupMachineId is not null || group.LookupDraftId is not null)
            .ToList();

        var (names, nameFailure) = await _machineNameFiller.LookupAsync(
            [.. unnamed.Select(group => group.LookupMachineId).OfType<string>().Distinct()],
            [.. unnamed.Select(group => group.LookupDraftId).OfType<string>().Distinct()],
            version,
            incoming,
            cancellationToken);

        if (names is null)
        {
            return ComposedResults.Problem(nameFailure!.Status, nameFailure.Problem);
        }

        foreach (var group in unnamed)
        {
            var machineId = group.LookupMachineId;
            var draftId = group.LookupDraftId;
            var isMachineKnown = machineId is not null && names.Machines.ContainsKey(machineId);

            group.Group[MachineNameProperty] = isMachineKnown
                ? names.Machines[machineId!]
                : draftId is not null && names.MachineDrafts.TryGetValue(draftId, out var draft)
                    ? draft
                    : null;
        }

        return Results.Json(
            new JsonObject { [GroupsProperty] = new JsonArray([.. groups.Select(group => group.Group)]) });
    }

    private static List<JsonObject> ReadObjects(DownstreamResult result, string property) =>
        result is DownstreamResult.Success success
            ? [.. (success.Body[property] as JsonArray ?? []).OfType<JsonObject>()]
            : [];
}
