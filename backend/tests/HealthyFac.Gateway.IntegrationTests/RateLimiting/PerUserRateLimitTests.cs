using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

[Collection(GatewayLimitsCollection.Name)]
public sealed class PerUserRateLimitTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task DefaultPolicy_TwoUsersFromOneAddress_HaveSeparateLimits()
    {
        await using var gateway = _environment.CreateGateway(_core, settings: RateLimitTestData.DefaultLimitOfTwo);
        using var firstUserClient = gateway.CreateAuthenticatedClient(new TestTokenClaims { Subject = Guid.NewGuid() });
        using var secondUserClient =
            gateway.CreateAuthenticatedClient(new TestTokenClaims { Subject = Guid.NewGuid() });
        await firstUserClient.SpendDefaultLimitAsync();

        using var firstUserResponse = await firstUserClient.GetAsync(
            RateLimitTestData.MachinesRoute, TestContext.Current.CancellationToken);
        using var secondUserResponse = await secondUserClient.GetAsync(
            RateLimitTestData.MachinesRoute, TestContext.Current.CancellationToken);

        firstUserResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        secondUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
