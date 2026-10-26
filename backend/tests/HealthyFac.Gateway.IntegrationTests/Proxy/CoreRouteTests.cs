using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

public sealed class CoreRouteTests(GatewayEnvironment environment) : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string PublicPrefix = "/api/core";

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public Task InitializeAsync() => _core.StartAsync();

    public Task DisposeAsync() => _core.DisposeAsync().AsTask();

    [Fact]
    public async Task CoreRoute_WithVersionedPath_RemovesPublicPrefix()
    {
        await using var gateway = environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        var response = await client.GetAsync("/api/core/v1/machines");

        var forwardedRequest = _core.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be("/v1/machines");
    }

    [Fact]
    public async Task CoreRoute_SendsForwardedPrefix()
    {
        await using var gateway = environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync("/api/core/v1/machines");

        var forwardedRequest = _core.Requests.Single();

        forwardedRequest.Headers.Should().ContainKey(ForwardedPrefixHeader);
        forwardedRequest.Headers[ForwardedPrefixHeader].Should().Be(PublicPrefix);
    }

    [Fact]
    public async Task CoreRoute_WithForwardedHeadersInDestination_RestoresPublicPath()
    {
        await using var gateway = environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        var response = await client.GetAsync("/api/core/v1/machines");

        var publicPath = await response.Content.ReadAsStringAsync();

        publicPath.Should().Be("/api/core/v1/machines");
    }

    [Fact]
    public async Task CoreRoute_WithQueryString_ForwardsRewrittenPathAndQuery()
    {
        await using var gateway = environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        var response = await client.GetAsync("/api/core/v1/employee-invites/verify?page=2&size=10");

        var forwardedRequest = _core.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be("/v1/employee-invites/verify");
        forwardedRequest.Query.Should().Be("?page=2&size=10");
    }

    [Fact]
    public async Task UnknownPath_ReturnsNotFound()
    {
        await using var gateway = environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        var response = await client.GetAsync("/api/other/v1/x");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _core.RequestCount.Should().Be(0);
    }
}
