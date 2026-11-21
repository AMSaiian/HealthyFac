using System.Net.WebSockets;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using HealthyFac.Shared.Web.Realtime;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

/// <remarks>
/// The tests run the gateway on Kestrel: the test server brings its own WebSocket feature, and the framework origin
/// check of <c>WebSocketOptions.AllowedOrigins</c> runs only when the server has none.
/// </remarks>
[Collection(GatewayProxyCollection.Name)]
public sealed class RealtimeOriginTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string ForeignOrigin = "http://evil.healthyfac.test:4200";

    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Connect_FromOriginOutsideTheList_Returns403()
    {
        await using var gateway = _environment.CreateGateway(_core);

        var act = async () => await ConnectAsync(gateway, ForeignOrigin);

        await act.Should().ThrowAsync<WebSocketException>().WithMessage(expectedWildcardPattern: "*403*");
    }

    [Fact]
    public async Task Connect_FromListedOrigin_Succeeds()
    {
        await using var gateway = _environment.CreateGateway(_core);

        using var socket = await ConnectAsync(gateway, GatewayFactory.DefaultCorsOrigin);

        socket.State.Should().Be(WebSocketState.Open);
    }

    [Fact]
    public async Task Connect_WithoutOrigin_Succeeds()
    {
        await using var gateway = _environment.CreateGateway(_core);

        using var socket = await ConnectAsync(gateway, origin: null);

        socket.State.Should().Be(WebSocketState.Open);
    }

    private static async Task<ClientWebSocket> ConnectAsync(GatewayFactory gateway, string? origin)
    {
        gateway.UseKestrel(port: 0);
        using var client = gateway.CreateClient();
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        var uri = new UriBuilder(new Uri(client.BaseAddress!, RealtimeConstants.HubPath))
        {
            Scheme = Uri.UriSchemeWs,
        }.Uri;
        var socket = new ClientWebSocket();
        if (origin is not null)
        {
            socket.Options.SetRequestHeader(headerName: "Origin", origin);
        }

        try
        {
            await socket.ConnectAsync(RealtimeTestClient.WithToken(uri, token), TestContext.Current.CancellationToken);
        }
        catch
        {
            socket.Dispose();

            throw;
        }

        return socket;
    }
}
