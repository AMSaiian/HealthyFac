namespace HealthyFac.Gateway.IntegrationTests.Hosting;

[CollectionDefinition(Name)]
public sealed class GatewayProxyCollection : ICollectionFixture<GatewayEnvironment>
{
    public const string Name = "GatewayProxy";
}
