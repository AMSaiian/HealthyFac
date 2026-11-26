using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using HealthyFac.Gateway.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

[Collection(GatewayProxyCollection.Name)]
[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Realtime)]
public sealed class RealtimeAuthenticationTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string Rejected = "*401*";

    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Connect_WithTokenInQuery_Succeeds()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);

        client.IsClosed.Should().BeFalse();
    }

    [Fact]
    public async Task Connect_WithTokenWithoutEmployee_DoesNotComplete()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()) with { EmployeeId = null });

        var act = async () => await RealtimeTestClient.ConnectAsync(gateway, token);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Connect_WithoutToken_Returns401()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var webSocketClient = RealtimeTestData.CreateWebSocketClient(gateway);

        var act = async () => await webSocketClient.ConnectAsync(
            RealtimeTestClient.HubUri(gateway),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Rejected);
    }

    [Fact]
    public async Task Connect_WithRevokedToken_Returns401()
    {
        var subject = Guid.NewGuid();
        await using var gateway = _environment.CreateGateway(_core);

        await gateway.Services.GetRequiredService<ITokenRevocationStore>()
            .RevokeAsync(
                subject,
                DateTimeOffset.UtcNow - RevocationTestData.RevocationAge,
                RevocationTestData.RevocationTimeToLive,
                CancellationToken.None);

        var token = gateway.Tokens.CreateToken(
            RealtimeTestData.Claims(subject) with
            {
                IssuedAt = DateTimeOffset.UtcNow - RevocationTestData.TokenAge,
                Lifetime = TimeSpan.FromHours(hours: 1),
            });

        var webSocketClient = RealtimeTestData.CreateWebSocketClient(gateway);

        var act = async () => await webSocketClient.ConnectAsync(
            RealtimeTestClient.WithToken(RealtimeTestClient.HubUri(gateway), token),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Rejected);
    }

    [Fact]
    public async Task Get_OfProxiedPathWithTokenInQuery_Returns401()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            $"{RevocationTestData.CoreRoute}?access_token={token}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _core.RequestCount.Should().Be(expected: 0);
    }
}
