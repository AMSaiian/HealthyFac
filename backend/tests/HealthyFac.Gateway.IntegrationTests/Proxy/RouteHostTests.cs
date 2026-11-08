using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class RouteHostTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly EchoServer _keycloak = new();

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _keycloak.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _keycloak.DisposeAsync();
    }

    [Fact]
    public async Task CoreRoute_OnApiHost_Forwards()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(RouteHostTestData.CoreUrl, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _core.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task CoreRoute_OnAuthHost_ReturnsNotFound()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            RouteHostTestData.CoreUrlOnAuthHost, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task KeycloakRoute_OnAuthHost_Forwards()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            RouteHostTestData.KeycloakUrl, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _keycloak.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task KeycloakRoute_OnApiHost_ReturnsNotFound()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            RouteHostTestData.KeycloakUrlOnApiHost, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _keycloak.RequestCount.Should().Be(expected: 0);
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(_core, settings: RouteHostTestData.CreateSettings(_keycloak.Address));
}
