using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases;

[CollectionDefinition(Name)]
public sealed class TelemetryPhasesCollection : ICollectionFixture<TelemetryDatabaseFixture>
{
    public const string Name = "TelemetryPhases";
}
