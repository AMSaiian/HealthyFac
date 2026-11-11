using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class InsightsRouteTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string PublicPrefix = "/api/insights";

    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly EchoServer _insights = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _insights.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _insights.DisposeAsync();
    }

    [Fact]
    public async Task InsightsRoute_WithVersionedPath_ForwardsToInsightsOnly()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            InsightsRouteTestData.VersionedPath, TestContext.Current.CancellationToken);

        var forwardedRequest = _insights.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be(expected: "/v1/files/abc/ingest-attempts");
        forwardedRequest.Headers[ForwardedPrefixHeader].Should().Be(PublicPrefix);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Theory]
    [MemberData(nameof(InsightsRouteTestData.UnversionedPaths), MemberType = typeof(InsightsRouteTestData))]
    public async Task InsightsRoute_WithUnversionedPath_DoesNotForward(string path)
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _insights.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task InsightsRoute_WithActorHeadersFromClient_RemovesThem()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add(ActorHeaderNames.ActorId, InsightsRouteTestData.ForgedActorId);
        client.DefaultRequestHeaders.Add(ActorHeaderNames.CompanyId, InsightsRouteTestData.ForgedCompanyId);
        client.DefaultRequestHeaders.Add(InsightsRouteTestData.KeptHeader, InsightsRouteTestData.KeptValue);

        using var response = await client.GetAsync(
            InsightsRouteTestData.VersionedPath, TestContext.Current.CancellationToken);

        var forwardedRequest = _insights.Requests.Single();

        forwardedRequest.Headers.Should().ContainKey(InsightsRouteTestData.KeptHeader);
        forwardedRequest.Headers.Should().NotContainKey(ActorHeaderNames.ActorId);
        forwardedRequest.Headers.Should().NotContainKey(ActorHeaderNames.CompanyId);
    }

    [Fact]
    public async Task InsightsRoute_WithoutToken_ReturnsUnauthorized()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        var response = await client.GetAsync(
            InsightsRouteTestData.VersionedPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _insights.RequestCount.Should().Be(expected: 0);
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(
            _core,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.InsightsAddress] = _insights.Address,
                [GatewaySettingKeys.InsightsHealthAddress] = _insights.Address,
            });
}
