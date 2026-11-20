using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

[Collection(GatewayProxyCollection.Name)]
public sealed class RealtimeOriginTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string Forbidden = "*403*";
    private const string ForeignOrigin = "http://evil.healthyfac.test:4200";

    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Connect_FromOriginOutsideTheList_Returns403()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        var webSocketClient = RealtimeTestData.CreateWebSocketClient(gateway);
        webSocketClient.ConfigureRequest = request => request.Headers[HeaderNames.Origin] = ForeignOrigin;

        var act = async () => await webSocketClient.ConnectAsync(
            RealtimeTestClient.WithToken(RealtimeTestClient.HubUri(gateway), token),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Forbidden);
    }

    [Fact]
    public async Task Connect_WithoutOrigin_Returns403()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        var webSocketClient = gateway.Server.CreateWebSocketClient();

        var act = async () => await webSocketClient.ConnectAsync(
            RealtimeTestClient.WithToken(RealtimeTestClient.HubUri(gateway), token),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(Forbidden);
    }

    [Fact]
    public async Task Connect_FromListedOrigin_Succeeds()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);

        client.IsClosed.Should().BeFalse();
    }
}
