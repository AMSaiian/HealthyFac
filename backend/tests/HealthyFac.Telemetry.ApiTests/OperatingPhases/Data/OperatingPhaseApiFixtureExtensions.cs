using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.OperatingPhases.Data;

public static class OperatingPhaseApiFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public string OperatingPhasesPath(string encryptedMachineId) =>
            $"{OperatingPhaseApiTestData.MachinesPath}/{encryptedMachineId}"
            + $"/{OperatingPhaseApiTestData.OperatingPhasesSegment}";

        public string OperatingPhaseBody(string code, string encryptedStatusId, int priority) =>
            new JsonObject
            {
                [propertyName: "code"] = code,
                [propertyName: "name"] = code,
                [propertyName: "priority"] = priority,
                [propertyName: "isTracked"] = true,
                [propertyName: "conditions"] = new JsonArray(
                    new JsonObject
                    {
                        [propertyName: "statusDefinitionId"] = encryptedStatusId,
                        [propertyName: "operator"] = "active",
                    }),
                [propertyName: "limits"] = new JsonArray(),
            }.ToJsonString();

        public async Task<EncryptedStatusDefinition> ArrangePhaseMachineAsync() =>
            await fixture.ArrangeStatusDefinitionAsync(OperatingPhaseApiTestData.StatusCode);
    }
}
