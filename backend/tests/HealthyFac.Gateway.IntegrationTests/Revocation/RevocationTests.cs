using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Revocation;

[Collection(GatewayLimitsCollection.Name)]
public sealed class RevocationTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Revocation_TokenIssuedBefore_Returns401()
    {
        var userId = Guid.NewGuid();
        await using var gateway = _environment.CreateGateway(_core);
        await RevokeAsync(gateway, userId);
        using var client = gateway.CreateAuthenticatedClient(
            RevocationTestData.CreateClaims(userId, RevocationTestData.TokenAge));

        using var response = await client.GetAsync(RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Revocation_TokenIssuedAfter_Passes()
    {
        var userId = Guid.NewGuid();
        await using var gateway = _environment.CreateGateway(_core);
        await RevokeAsync(gateway, userId);
        using var client = gateway.CreateAuthenticatedClient(
            RevocationTestData.CreateClaims(userId, RevocationTestData.TokenYoungAge));

        using var response = await client.GetAsync(RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _core.RequestCount.Should().Be(expected: 1);
    }

    private static Task RevokeAsync(GatewayFactory gateway, Guid userId) =>
        gateway.Services.GetRequiredService<ITokenRevocationStore>()
            .RevokeAsync(
                userId,
                DateTimeOffset.UtcNow - RevocationTestData.RevocationAge,
                RevocationTestData.RevocationTimeToLive,
                CancellationToken.None);
}
