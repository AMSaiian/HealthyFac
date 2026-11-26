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
                ["code"] = code,
                ["name"] = code,
                ["priority"] = priority,
                ["isTracked"] = true,
                ["conditions"] = new JsonArray(
                    new JsonObject
                    {
                        ["statusDefinitionId"] = encryptedStatusId,
                        ["operator"] = "active",
                    }),
                ["limits"] = new JsonArray(),
            }.ToJsonString();

        public async Task<EncryptedStatusDefinition> ArrangePhaseMachineAsync() =>
            await fixture.ArrangeStatusDefinitionAsync(OperatingPhaseApiTestData.StatusCode);
    }
}
