using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[CollectionDefinition(Name)]
public sealed class TelemetryDefinitionDraftsCollection : ICollectionFixture<TelemetryDatabaseFixture>
{
    public const string Name = "TelemetryDefinitionDrafts";
}
