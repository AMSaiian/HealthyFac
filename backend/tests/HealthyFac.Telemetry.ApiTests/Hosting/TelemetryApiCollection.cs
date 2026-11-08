namespace HealthyFac.Telemetry.ApiTests.Hosting;

[CollectionDefinition(Name)]
public sealed class TelemetryApiCollection : ICollectionFixture<TelemetryApiFixture>
{
    public const string Name = "TelemetryApi";
}
