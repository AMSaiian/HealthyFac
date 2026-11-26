using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Connects to the hub of a <see cref="GatewayFactory"/> the way the web app does: WebSockets only, no negotiate, the
/// token in the query, and an <c>Origin</c> header. It records each message as the raw JSON that the wire carries.
/// </summary>
public sealed class RealtimeTestClient : IAsyncDisposable
{
    public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(seconds: 10);
    private static readonly TimeSpan PushRetryInterval = TimeSpan.FromMilliseconds(milliseconds: 500);

    public static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(seconds: 2);

    private readonly HubConnection _connection;
    private readonly Channel<JsonElement> _notifications = Channel.CreateUnbounded<JsonElement>();
    private readonly Channel<JsonElement> _hints = Channel.CreateUnbounded<JsonElement>();

    private RealtimeTestClient(HubConnection connection)
    {
        _connection = connection;

        _connection.On<JsonElement>(
            nameof(IRealtimeClient.Notification), message => _notifications.Writer.TryWrite(message));

        _connection.On<JsonElement>(nameof(IRealtimeClient.Hint), message => _hints.Writer.TryWrite(message));
        _connection.On(nameof(IRealtimeClient.SessionEnded), () => SessionEndedCount++);

        _connection.Closed += error =>
        {
            IsClosed = true;
            CloseError = error;

            return Task.CompletedTask;
        };
    }

    public int SessionEndedCount { get; private set; }

    public bool IsClosed { get; private set; }

    public Exception? CloseError { get; private set; }

    public static async Task<RealtimeTestClient> ConnectAsync(
        GatewayFactory gateway,
        string token,
        string origin = GatewayFactory.DefaultCorsOrigin
    )
    {
        var connection = BuildConnection(gateway, token, origin);
        var client = new RealtimeTestClient(connection);
        await connection.StartAsync(TestContext.Current.CancellationToken);

        return client;
    }

    public Task<JsonElement?> ReceiveNotificationAsync(TimeSpan timeout) => ReceiveAsync(_notifications, timeout);

    public Task<JsonElement?> ReceiveHintAsync(TimeSpan timeout) => ReceiveAsync(_hints, timeout);

    /// <summary>
    /// Pushes in a loop until a notification arrives or <see cref="ReceiveTimeout"/> ends. The hub subscribes the
    /// connection to Redis after the handshake, so the first push can reach Redis before the subscription.
    /// </summary>
    public Task<JsonElement?> ReceiveNotificationAfterPushingAsync(Func<Task> push) =>
        PushUntilReceivedAsync(push, _notifications);

    /// <summary>
    /// Pushes in a loop until a hint arrives or <see cref="ReceiveTimeout"/> ends. The hub joins the groups after the
    /// handshake, so the first push can precede the join.
    /// </summary>
    public Task<JsonElement?> ReceiveHintAfterPushingAsync(Func<Task> push) => PushUntilReceivedAsync(push, _hints);

    public async Task<bool> WaitForCloseAsync(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (!IsClosed && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100), TestContext.Current.CancellationToken);
        }

        return IsClosed;
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private static HubConnection BuildConnection(GatewayFactory gateway, string token, string origin)
    {
        var webSocketClient = gateway.Server.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request => request.Headers.Origin = origin;

        return new HubConnectionBuilder()
            .WithUrl(
                HubUri(gateway),
                options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;

                    options.WebSocketFactory = (context, cancellationToken) =>
                        new ValueTask<WebSocket>(
                            webSocketClient.ConnectAsync(WithToken(context.Uri, token), cancellationToken));
                })
            .Build();
    }

    public static Uri HubUri(GatewayFactory gateway) => new(gateway.Server.BaseAddress, RealtimeConstants.HubPath);

    public static Uri WithToken(Uri uri, string token) =>
        new UriBuilder(uri)
        {
            Query = $"{RealtimeConstants.AccessTokenQueryKey}={Uri.EscapeDataString(token)}",
        }.Uri;

    private static async Task<JsonElement?> PushUntilReceivedAsync(Func<Task> push, Channel<JsonElement> channel)
    {
        var deadline = DateTimeOffset.UtcNow + ReceiveTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            await push();
            var received = await ReceiveAsync(channel, PushRetryInterval);

            if (received is not null)
            {
                return received;
            }
        }

        return null;
    }

    private static async Task<JsonElement?> ReceiveAsync(Channel<JsonElement> channel, TimeSpan timeout)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.CancelAfter(timeout);

        try
        {
            return await channel.Reader.ReadAsync(source.Token);
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
