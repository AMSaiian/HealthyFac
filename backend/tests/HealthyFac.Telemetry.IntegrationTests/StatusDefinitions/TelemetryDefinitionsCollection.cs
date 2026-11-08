using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

[CollectionDefinition(Name)]
public sealed class TelemetryDefinitionsCollection : ICollectionFixture<TelemetryDatabaseFixture>
{
    public const string Name = "TelemetryDefinitions";
}
