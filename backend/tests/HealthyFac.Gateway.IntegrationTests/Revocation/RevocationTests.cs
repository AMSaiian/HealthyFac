using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Revocation;

public sealed class RevocationTests(GatewayEnvironment environment)
    : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public Task InitializeAsync() => _core.StartAsync();

    public Task DisposeAsync() => _core.DisposeAsync().AsTask();

    [Fact]
    public async Task Revocation_TokenIssuedBefore_Returns401()
    {
        var userId = Guid.NewGuid();
        await using var gateway = environment.CreateGateway(_core);
        await RevokeAsync(gateway, userId);
        using var client = gateway.CreateAuthenticatedClient(
            RevocationTestData.CreateClaims(userId, RevocationTestData.TokenAge));

        using var response = await client.GetAsync(RevocationTestData.CoreRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _core.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task Revocation_TokenIssuedAfter_Passes()
    {
        var userId = Guid.NewGuid();
        await using var gateway = environment.CreateGateway(_core);
        await RevokeAsync(gateway, userId);
        using var client = gateway.CreateAuthenticatedClient(
            RevocationTestData.CreateClaims(userId, RevocationTestData.TokenYoungAge));

        using var response = await client.GetAsync(RevocationTestData.CoreRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _core.RequestCount.Should().Be(1);
    }

    private static Task RevokeAsync(GatewayFactory gateway, Guid userId) =>
        gateway.Services.GetRequiredService<ITokenRevocationStore>()
            .RevokeAsync(
                userId,
                DateTimeOffset.UtcNow - RevocationTestData.RevocationAge,
                RevocationTestData.RevocationTimeToLive,
                CancellationToken.None);
}
