using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class AttemptDraftGrouperTestData
{
    internal static readonly JsonObject MachineGroup =
        JsonNode.Parse(json: """{"machineId":"m1","machineName":"Lathe","machineDraft":{"id":"d1"}}""")!.AsObject();

    internal static readonly JsonObject CreateGroup =
        JsonNode.Parse(json: """{"machineId":null,"machineName":null,"machineDraft":{"id":"d2"}}""")!.AsObject();

    public static JsonObject Draft(string id, string? targetMachineId, string? targetMachineDraftId, string entryId) =>
        new()
        {
            ["id"] = id,
            ["targetMachineId"] = targetMachineId,
            ["targetMachineDraftId"] = targetMachineDraftId,
            ["proposalEntryId"] = entryId,
        };
}
