using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>The names that Core returned: the machine names and the proposed names of the create drafts.</summary>
public sealed record MachineNames
{
    /// <summary>The machine names by encrypted machine ID.</summary>
    public required IReadOnlyDictionary<string, string> Machines { get; init; }

    /// <summary>The proposed names by encrypted create draft ID.</summary>
    public required IReadOnlyDictionary<string, string> MachineDrafts { get; init; }
}

/// <summary>
/// Reads machine names from Core for composed responses. Telemetry does not own machine names, so one call to Core
/// reads the names of the machines and of the create drafts that a page names.
/// </summary>
/// <param name="downstreamClient">Calls Core.</param>
public sealed class MachineNameFiller(DownstreamClient downstreamClient)
{
    private const string MachineIdProperty = "machineId";
    private const string TargetMachineDraftIdProperty = "targetMachineDraftId";
    private const string MachineNameProperty = "machineName";
    private const string KindProperty = "kind";
    private const string MachineIdsProperty = "machineIds";
    private const string MachineDraftIdsProperty = "machineDraftIds";
    private const string MachinesProperty = "machines";
    private const string MachineDraftsProperty = "machineDrafts";
    private const string IdProperty = "id";
    private const string NameProperty = "name";

    private readonly DownstreamClient _downstreamClient = downstreamClient;

    /// <summary>
    /// Reads names from Core. It makes no call when no ID is given.
    /// </summary>
    /// <param name="machineIds">The encrypted machine IDs.</param>
    /// <param name="draftIds">The encrypted create draft IDs.</param>
    /// <param name="version">The API version of the request, for the Core path.</param>
    /// <param name="incoming">The request of the person, for the <c>Authorization</c> header.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The names, or the failure that the call gave.</returns>
    public async Task<(MachineNames? Names, ComposedOutcome.Failure? Failure)> LookupAsync(
        IReadOnlyCollection<string> machineIds,
        IReadOnlyCollection<string> draftIds,
        string version,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        if (machineIds.Count == 0 && draftIds.Count == 0)
        {
            return (
                new MachineNames
                {
                    Machines = new Dictionary<string, string>(),
                    MachineDrafts = new Dictionary<string, string>(),
                },
                null);
        }

        var body = new JsonObject
        {
            [MachineIdsProperty] = new JsonArray([.. machineIds.Select(id => JsonValue.Create(id)!)]),
            [MachineDraftIdsProperty] = new JsonArray([.. draftIds.Select(id => JsonValue.Create(id)!)]),
        };

        var result = await _downstreamClient.SendAsync(
            ComposedClusters.Core,
            HttpMethod.Post,
            DraftCompositionRoutes.CoreMachineNames(version),
            body,
            incoming,
            cancellationToken);

        var outcome = ComposedErrors.Combine([result]);

        if (outcome is ComposedOutcome.Failure failure)
        {
            return (null, failure);
        }

        var names = ((ComposedOutcome.Success)outcome).Bodies.Single();

        return (
            new MachineNames
            {
                Machines = ReadNames(names[MachinesProperty]),
                MachineDrafts = ReadNames(names[MachineDraftsProperty]),
            },
            null);
    }

    /// <summary>
    /// Sets <c>machineName</c> on every row that Telemetry supplied. It makes no call when no such row names a machine
    /// or a create draft.
    /// </summary>
    /// <param name="rows">The rows of the merged page. The method changes the rows that Telemetry supplied.</param>
    /// <param name="version">The API version of the request, for the Core path.</param>
    /// <param name="incoming">The request of the person, for the <c>Authorization</c> header.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A failure when the call failed, else <see langword="null"/>.</returns>
    public async Task<ComposedOutcome.Failure?> FillAsync(
        IReadOnlyList<JsonObject> rows,
        string version,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        var telemetryRows = rows.Where(row => ReadText(row, KindProperty) != DraftNames.Kinds.Machine).ToList();
        var machineIds = telemetryRows
            .Select(row => ReadText(row, MachineIdProperty))
            .OfType<string>()
            .Distinct()
            .ToList();
        var draftIds = telemetryRows
            .Select(row => ReadText(row, TargetMachineDraftIdProperty))
            .OfType<string>()
            .Distinct()
            .ToList();

        var (names, failure) = await LookupAsync(machineIds, draftIds, version, incoming, cancellationToken);

        if (names is null)
        {
            return failure;
        }

        foreach (var row in telemetryRows)
        {
            var machineId = ReadText(row, MachineIdProperty);
            var draftId = ReadText(row, TargetMachineDraftIdProperty);

            row[MachineNameProperty] = machineId is not null && names.Machines.TryGetValue(machineId, out var machine)
                ? machine
                : draftId is not null && names.MachineDrafts.TryGetValue(draftId, out var draft)
                    ? draft
                    : null;
        }

        return null;
    }

    private static string? ReadText(JsonObject row, string property) =>
        row[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static Dictionary<string, string> ReadNames(JsonNode? array) =>
        (array as JsonArray ?? [])
        .OfType<JsonObject>()
        .Where(item => ReadText(item, IdProperty) is not null && ReadText(item, NameProperty) is not null)
        .ToDictionary(item => ReadText(item, IdProperty)!, item => ReadText(item, NameProperty)!);
}
