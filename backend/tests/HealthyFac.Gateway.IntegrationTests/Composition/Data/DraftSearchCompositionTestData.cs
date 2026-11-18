using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

public static class DraftSearchCompositionTestData
{
    public const string MachineRouteId = "v1.machine";
    public const string PublicSearchPath = "/api/core/v1/machines/" + MachineRouteId + "/drafts/search";
    public const string CoreSearchPath = "/v1/machines/" + MachineRouteId + "/drafts/search";
    public const string TelemetrySearchPath = "/v1/machines/" + MachineRouteId + "/definition-drafts/search";
    public const string NamesPath = "/v1/machines/names";
    public const string KnownMachineId = "v1.known";
    public const string CreateDraftId = "v1.create";
    public const string KnownMachineName = "Lathe 7";
    public const string CreateDraftName = "Press 9";
    public const int PageLimit = 2;

    /// <summary>The limits of the call count test: one below and one above the rows of a source.</summary>
    public static TheoryData<int> Limits => new() { 5, 50 };

    public static IReadOnlyList<StubDraftRow> CoreRows =>
    [
        CoreRow(id: "c1", stateRank: 0),
        CoreRow(id: "c2", stateRank: 0),
        CoreRow(id: "c3", stateRank: 2),
    ];

    public static IReadOnlyList<StubDraftRow> TelemetryRows =>
    [
        TelemetryRow(id: "t1", kind: "metric", stateRank: 0, KnownMachineId),
        TelemetryRow(id: "t2", kind: "status", stateRank: 1, targetMachineDraftId: CreateDraftId),
        TelemetryRow(id: "t3", kind: "metric", stateRank: 1, KnownMachineId),
        TelemetryRow(id: "t4", kind: "status", stateRank: 3, KnownMachineId),
    ];

    /// <summary>The IDs of the rows above in the merged order (the state rank, then Core first, then the ID).</summary>
    public static readonly string[] MergedIds = ["c1", "c2", "t1", "t2", "t3", "c3", "t4"];

    public const int LongListLimit = 10;
    public const int LongListTotal = 23;
    private const int LongListTelemetryCount = 20;

    /// <summary>Core finishes on the first page of the long list: it holds fewer rows than the limit.</summary>
    public static IReadOnlyList<StubDraftRow> LongListCoreRows => CoreRows
        .Select(row => row with { StateRank = 0 })
        .ToList();

    /// <summary>Telemetry holds twice the limit, so it fills the pages after the first.</summary>
    public static IReadOnlyList<StubDraftRow> LongListTelemetryRows =>
    [
        .. Enumerable.Range(start: 1, LongListTelemetryCount)
            .Select(number => TelemetryRow(
                $"t{number:D2}",
                kind: "metric",
                stateRank: 0,
                KnownMachineId)),
    ];

    public const string NamesJson =
        "{\"machines\":[{\"id\":\""
        + KnownMachineId
        + "\",\"name\":\""
        + KnownMachineName
        + "\"}],"
        + "\"machineDrafts\":[{\"id\":\""
        + CreateDraftId
        + "\",\"name\":\""
        + CreateDraftName
        + "\"}]}";

    private static StubDraftRow CoreRow(string id, int stateRank) =>
        new(id, Kind: "machine", stateRank, new JsonObject { [propertyName: "machineName"] = "Core machine" });

    private static StubDraftRow TelemetryRow(
        string id,
        string kind,
        int stateRank,
        string? machineId = null,
        string? targetMachineDraftId = null
    ) =>
        new(
            id,
            kind,
            stateRank,
            new JsonObject
            {
                [propertyName: "machineId"] = machineId,
                [propertyName: "targetMachineDraftId"] = targetMachineDraftId,
            });
}
