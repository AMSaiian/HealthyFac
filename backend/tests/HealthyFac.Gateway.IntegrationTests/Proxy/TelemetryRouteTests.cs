using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class TelemetryRouteTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string PublicPrefix = "/api/telemetry";

    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly EchoServer _telemetry = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _telemetry.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _telemetry.DisposeAsync();
    }

    [Fact]
    public async Task TelemetryRoute_WithVersionedPath_ForwardsToTelemetryOnly()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            requestUri: "/api/telemetry/v1/machines/abc/metric-definitions", TestContext.Current.CancellationToken);

        var forwardedRequest = _telemetry.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be(expected: "/v1/machines/abc/metric-definitions");
        forwardedRequest.Headers[ForwardedPrefixHeader].Should().Be(PublicPrefix);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Theory]
    [MemberData(nameof(TelemetryRouteTestData.UnversionedPaths), MemberType = typeof(TelemetryRouteTestData))]
    public async Task TelemetryRoute_WithUnversionedPath_DoesNotForward(string path)
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _telemetry.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task TelemetryRoute_WithoutToken_ReturnsUnauthorized()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        var response = await client.GetAsync(
            requestUri: "/api/telemetry/v1/machines/abc/metric-definitions", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _telemetry.RequestCount.Should().Be(expected: 0);
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(
            _core,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.TelemetryAddress] = _telemetry.Address,
                [GatewaySettingKeys.TelemetryHealthAddress] = _telemetry.Address,
            });
}
