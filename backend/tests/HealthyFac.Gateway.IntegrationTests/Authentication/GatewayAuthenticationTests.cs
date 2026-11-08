using System.Net;
using HealthyFac.Gateway.IntegrationTests.Authentication.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Gateway.IntegrationTests.Authentication;

[Collection(GatewayProxyCollection.Name)]
public sealed class GatewayAuthenticationTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task CoreRoute_WithoutToken_Returns401()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            AuthenticationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task CoreRoute_WithWrongAudience_Returns401()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient(
            new TestTokenClaims { Audience = AuthenticationTestData.OtherAudience });

        using var response = await client.GetAsync(
            AuthenticationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task CoreRoute_WithValidToken_Forwards()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            AuthenticationTestData.CoreRoute, TestContext.Current.CancellationToken);

        var forwardedRequest = _core.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Headers.Should().ContainKey(AuthenticationTestData.AuthorizationHeader);
    }

    [Fact]
    public async Task InvitePublicRoute_WithoutToken_Forwards()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateClient();

        using var response = await client.PostAsync(
            AuthenticationTestData.VerifyInviteRoute, content: null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _core.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task AdminPath_ReturnsNotFound()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            AuthenticationTestData.AdminRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _core.RequestCount.Should().Be(expected: 0);
    }
}
