using System.Net;
using HealthyFac.Gateway.IntegrationTests.CrossOrigin.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

namespace HealthyFac.Gateway.IntegrationTests.CrossOrigin;

public sealed class GatewayCorsTests(GatewayEnvironment environment) : IClassFixture<GatewayEnvironment>, IAsyncLifetime
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
    public async Task CorePreflight_WithoutToken_Returns204WithAllowOrigin()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.SendPreflightAsync(GatewayCorsTestData.CoreInvitesRoute);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues(GatewayCorsTestData.AllowOriginHeader)
            .Should()
            .Equal(GatewayFactory.DefaultCorsOrigin);

        _core.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task InvitePreflight_ForPostOnlyRoute_Returns204()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.SendPreflightAsync(GatewayCorsTestData.VerifyInviteRoute);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues(GatewayCorsTestData.AllowOriginHeader)
            .Should()
            .Equal(GatewayFactory.DefaultCorsOrigin);
    }

    [Fact]
    public async Task CoreRequest_FromAllowedOrigin_HasOneAllowOrigin()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetFromOriginAsync(GatewayCorsTestData.MachinesRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(GatewayCorsTestData.AllowOriginHeader)
            .Should()
            .Equal(GatewayFactory.DefaultCorsOrigin);
    }

    [Fact]
    public async Task CorePreflight_AboveDefaultLimit_DoesNotUseLimitOfAddress()
    {
        await using var gateway = _environment.CreateGateway(_core, settings: RateLimitTestData.DefaultLimitOfTwo);
        using var client = gateway.CreateClient();
        var preflightStatusCodes = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < GatewayCorsTestData.PreflightCount; attempt++)
        {
            using var preflight = await client.SendPreflightAsync(GatewayCorsTestData.CoreInvitesRoute);
            preflightStatusCodes.Add(preflight.StatusCode);
        }

        using var response = await client.GetAsync(GatewayCorsTestData.MachinesRoute);

        preflightStatusCodes.Should().OnlyContain(statusCode => statusCode == HttpStatusCode.NoContent);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenPreflight_IsForwardedToKeycloak()
    {
        await using var gateway = _environment.CreateGateway(
            _core,
            settings: GatewayCorsTestData.CreateKeycloakSettings(_keycloak.Address));
        using var client = gateway.CreateClient();

        using var response = await client.SendPreflightAsync(GatewayCorsTestData.TokenRoute);

        var forwardedRequest = _keycloak.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(GatewayCorsTestData.AllowOriginHeader).Should().BeFalse();
        forwardedRequest.Method.Should().Be(HttpMethod.Options.Method);
    }
}
