using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

public sealed class PerUserRateLimitTests(GatewayEnvironment environment)
    : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public Task InitializeAsync() => _core.StartAsync();

    public Task DisposeAsync() => _core.DisposeAsync().AsTask();

    [Fact]
    public async Task DefaultPolicy_TwoUsersFromOneAddress_HaveSeparateLimits()
    {
        await using var gateway = environment.CreateGateway(_core, settings: RateLimitTestData.DefaultLimitOfTwo);
        using var firstUserClient = gateway.CreateAuthenticatedClient(new TestTokenClaims { Subject = Guid.NewGuid() });
        using var secondUserClient =
            gateway.CreateAuthenticatedClient(new TestTokenClaims { Subject = Guid.NewGuid() });
        await firstUserClient.SpendDefaultLimitAsync();

        using var firstUserResponse = await firstUserClient.GetAsync(RateLimitTestData.MachinesRoute);
        using var secondUserResponse = await secondUserClient.GetAsync(RateLimitTestData.MachinesRoute);

        firstUserResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        secondUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
