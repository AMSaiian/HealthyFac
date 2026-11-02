using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

public sealed class RouteHostTests(GatewayEnvironment environment) : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly EchoServer _keycloak = new();

    public async Task InitializeAsync()
    {
        await _core.StartAsync();
        await _keycloak.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _core.DisposeAsync();
        await _keycloak.DisposeAsync();
    }

    [Fact]
    public async Task CoreRoute_OnApiHost_Forwards()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(RouteHostTestData.CoreUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _core.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task CoreRoute_OnAuthHost_ReturnsNotFound()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(RouteHostTestData.CoreUrlOnAuthHost);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _core.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task KeycloakRoute_OnAuthHost_Forwards()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(RouteHostTestData.KeycloakUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _keycloak.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task KeycloakRoute_OnApiHost_ReturnsNotFound()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(RouteHostTestData.KeycloakUrlOnApiHost);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _keycloak.RequestCount.Should().Be(0);
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(_core, settings: RouteHostTestData.CreateSettings(_keycloak.Address));
}
