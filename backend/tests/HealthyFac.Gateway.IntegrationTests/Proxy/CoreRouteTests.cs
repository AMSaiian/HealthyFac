using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class CoreRouteTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string PublicPrefix = "/api/core";

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task CoreRoute_WithVersionedPath_RemovesPublicPrefix()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            requestUri: "/api/core/v1/machines", TestContext.Current.CancellationToken);

        var forwardedRequest = _core.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be(expected: "/v1/machines");
    }

    [Fact]
    public async Task CoreRoute_SendsForwardedPrefix()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            requestUri: "/api/core/v1/machines", TestContext.Current.CancellationToken);

        var forwardedRequest = _core.Requests.Single();

        forwardedRequest.Headers.Should().ContainKey(ForwardedPrefixHeader);
        forwardedRequest.Headers[ForwardedPrefixHeader].Should().Be(PublicPrefix);
    }

    [Fact]
    public async Task CoreRoute_WithForwardedHeadersInDestination_RestoresPublicPath()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            requestUri: "/api/core/v1/machines", TestContext.Current.CancellationToken);

        var publicPath = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        publicPath.Should().Be(expected: "/api/core/v1/machines");
    }

    [Fact]
    public async Task CoreRoute_WithQueryString_ForwardsRewrittenPathAndQuery()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            requestUri: "/api/core/v1/employee-invites/verify?page=2&size=10", TestContext.Current.CancellationToken);

        var forwardedRequest = _core.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be(expected: "/v1/employee-invites/verify");
        forwardedRequest.Query.Should().Be(expected: "?page=2&size=10");
    }

    [Theory]
    [MemberData(nameof(CoreRouteTestData.UnversionedPaths), MemberType = typeof(CoreRouteTestData))]
    public async Task CoreRoute_WithUnversionedPath_DoesNotForward(string path)
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task UnknownPath_ReturnsNotFound()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(requestUri: "/api/other/v1/x", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _core.RequestCount.Should().Be(expected: 0);
    }
}
