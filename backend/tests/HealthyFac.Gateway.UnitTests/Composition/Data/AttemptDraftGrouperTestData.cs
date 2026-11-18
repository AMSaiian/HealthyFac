using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class AttemptDraftGrouperTestData
{
    public static readonly JsonObject MachineGroup =
        JsonNode.Parse(json: """{"machineId":"m1","machineName":"Lathe","machineDraft":{"id":"d1"}}""")!.AsObject();

    public static readonly JsonObject CreateGroup =
        JsonNode.Parse(json: """{"machineId":null,"machineName":null,"machineDraft":{"id":"d2"}}""")!.AsObject();

    public static JsonObject Draft(string id, string? targetMachineId, string? targetMachineDraftId, string entryId) =>
        new()
        {
            [propertyName: "id"] = id,
            [propertyName: "targetMachineId"] = targetMachineId,
            [propertyName: "targetMachineDraftId"] = targetMachineDraftId,
            [propertyName: "proposalEntryId"] = entryId,
        };
}
