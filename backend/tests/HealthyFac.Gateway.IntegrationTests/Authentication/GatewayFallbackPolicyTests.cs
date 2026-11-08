using System.Net;
using HealthyFac.Gateway.IntegrationTests.Authentication.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Authentication;

[Collection(GatewayProxyCollection.Name)]
public sealed class GatewayFallbackPolicyTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Route_WithoutAuthorizationPolicy_WithoutToken_Returns401()
    {
        await using var gateway = _environment.CreateGateway(
            _core,
            settings: FallbackPolicyTestData.UnprotectedRouteSettings);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            FallbackPolicyTestData.UnprotectedRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Route_WithoutAuthorizationPolicy_WithToken_Forwards()
    {
        await using var gateway = _environment.CreateGateway(
            _core,
            settings: FallbackPolicyTestData.UnprotectedRouteSettings);
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            FallbackPolicyTestData.UnprotectedRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _core.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task UnknownPath_WithoutToken_Returns401()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            FallbackPolicyTestData.UnknownRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _core.RequestCount.Should().Be(expected: 0);
    }
}
