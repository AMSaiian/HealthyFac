using HealthyFac.Telemetry.ApiTests.Hosting;

namespace HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;

public static class MetricDefinitionApiFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public string DefinitionsPath(string encryptedMachineId) =>
            $"{MetricDefinitionApiTestData.MachinesPath}/{encryptedMachineId}"
            + $"/{MetricDefinitionApiTestData.DefinitionsSegment}";

        public string SchemaPath(string encryptedMachineId) =>
            $"{MetricDefinitionApiTestData.MachinesPath}/{encryptedMachineId}"
            + $"/{MetricDefinitionApiTestData.SchemaSegment}";
    }
}
