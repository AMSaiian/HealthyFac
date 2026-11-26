using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts.AttemptDrafts;

/// <summary>One group of the attempt view and the IDs that name its machine, for the name lookup.</summary>
public sealed record AttemptGroup
{
    /// <summary>The group as the client sees it.</summary>
    public required JsonObject Group { get; init; }

    /// <summary>The encrypted machine ID, when the group needs the name of the machine.</summary>
    public string? LookupMachineId { get; init; }

    /// <summary>The encrypted create draft ID, when the group needs its proposed name.</summary>
    public string? LookupDraftId { get; init; }
}

/// <summary>
/// Puts the Telemetry metric, status, and phase drafts of one ingest attempt into the groups that Core built from the
/// machine
/// drafts. A draft joins the group of its target machine, else the group of the create draft that it waits on, else a
/// group of its proposal entry. The entry ID is the ID of the create draft, so a draft without a target joins that
/// group.
/// </summary>
public static class AttemptDraftGrouper
{
    private const string MachineIdProperty = "machineId";
    private const string MachineNameProperty = "machineName";
    private const string MachineDraftProperty = "machineDraft";
    private const string MetricsProperty = "metrics";
    private const string StatusesProperty = "statuses";
    private const string PhasesProperty = "phases";
    private const string IdProperty = "id";
    private const string TargetMachineIdProperty = "targetMachineId";
    private const string TargetMachineDraftIdProperty = "targetMachineDraftId";
    private const string ProposalEntryIdProperty = "proposalEntryId";
    private const string MachineKey = "machine:";
    private const string DraftKey = "draft:";

    /// <summary>
    /// Builds the groups. The Core groups come first in their order, then the new groups in the order of the first
    /// draft that needs them.
    /// </summary>
    /// <param name="coreGroups">The groups of Core (<c>machineId</c>, <c>machineName</c>, <c>machineDraft</c>).</param>
    /// <param name="metrics">The Telemetry metric drafts of the attempt.</param>
    /// <param name="statuses">The Telemetry status drafts of the attempt.</param>
    /// <param name="phases">The Telemetry phase drafts of the attempt.</param>
    /// <returns>The groups, each with its <c>metrics</c>, <c>statuses</c>, and <c>phases</c>.</returns>
    public static IReadOnlyList<AttemptGroup> Group(
        IReadOnlyList<JsonObject> coreGroups,
        IReadOnlyList<JsonObject> metrics,
        IReadOnlyList<JsonObject> statuses,
        IReadOnlyList<JsonObject> phases
    )
    {
        var groups = new List<AttemptGroup>();
        var byKey = new Dictionary<string, AttemptGroup>();

        foreach (var coreGroup in coreGroups)
        {
            var group = (JsonObject)coreGroup.DeepClone();
            group[MetricsProperty] = new JsonArray();
            group[StatusesProperty] = new JsonArray();
            group[PhasesProperty] = new JsonArray();
            var entry = new AttemptGroup { Group = group };
            groups.Add(entry);
            byKey.TryAdd(KeyOfCoreGroup(group), entry);
        }

        foreach (var metric in metrics)
        {
            Add(groups, byKey, metric, MetricsProperty);
        }

        foreach (var status in statuses)
        {
            Add(groups, byKey, status, StatusesProperty);
        }

        foreach (var phase in phases)
        {
            Add(groups, byKey, phase, PhasesProperty);
        }

        return groups;
    }

    private static void Add(
        List<AttemptGroup> groups,
        Dictionary<string, AttemptGroup> byKey,
        JsonObject draft,
        string listProperty
    )
    {
        var machineId = ReadText(draft, TargetMachineIdProperty);

        var draftId = machineId is null
            ? ReadText(draft, TargetMachineDraftIdProperty) ?? ReadText(draft, ProposalEntryIdProperty)
            : null;

        var key = machineId is not null ? MachineKey + machineId : DraftKey + draftId;

        if (!byKey.TryGetValue(key, out var group))
        {
            group = new AttemptGroup
            {
                Group = new JsonObject
                {
                    [MachineIdProperty] = machineId,
                    [MachineNameProperty] = null,
                    [MachineDraftProperty] = null,
                    [MetricsProperty] = new JsonArray(),
                    [StatusesProperty] = new JsonArray(),
                    [PhasesProperty] = new JsonArray(),
                },
                LookupMachineId = machineId,
                LookupDraftId = draftId,
            };

            groups.Add(group);
            byKey[key] = group;
        }

        group.Group[listProperty]!.AsArray().Add(draft.DeepClone());
    }

    private static string KeyOfCoreGroup(JsonObject group)
    {
        var machineId = ReadText(group, MachineIdProperty);
        var draftId = group[MachineDraftProperty] is JsonObject draft ? ReadText(draft, IdProperty) : null;

        return machineId is not null ? MachineKey + machineId : DraftKey + draftId;
    }

    private static string? ReadText(JsonObject node, string property) =>
        node[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
