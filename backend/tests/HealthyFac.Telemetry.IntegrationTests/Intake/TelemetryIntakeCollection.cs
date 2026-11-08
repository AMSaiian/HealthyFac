using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

[CollectionDefinition(Name)]
public sealed class TelemetryIntakeCollection : ICollectionFixture<TelemetryDatabaseFixture>
{
    public const string Name = "TelemetryIntake";
}
