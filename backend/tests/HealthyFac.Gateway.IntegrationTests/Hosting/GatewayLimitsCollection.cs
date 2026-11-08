namespace HealthyFac.Gateway.IntegrationTests.Hosting;

[CollectionDefinition(Name)]
public sealed class GatewayLimitsCollection : ICollectionFixture<GatewayEnvironment>
{
    public const string Name = "GatewayLimits";
}
