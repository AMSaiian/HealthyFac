using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Database;

[CollectionDefinition(Name)]
public sealed class TelemetrySeedingCollection : ICollectionFixture<TelemetryDatabaseFixture>
{
    public const string Name = "TelemetrySeeding";
}
