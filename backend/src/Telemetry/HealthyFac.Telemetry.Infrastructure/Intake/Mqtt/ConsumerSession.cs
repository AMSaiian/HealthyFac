using MQTTnet;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// One MQTT connection of the intake with the state that its handlers share: the processing token source, the
/// in-flight tracker, and the end of the session. It wires the handlers, connects and checks the result, and
/// disconnects when it is disposed and the client is still connected.
/// </summary>
/// <remarks>
/// The processing token ends only at a <see cref="ProcessingTimeout"/>, never at the host stop token, so a message that
/// is already received can finish. Nothing in the class unsubscribes or deletes the session.
/// </remarks>
internal sealed class ConsumerSession : IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly CancellationTokenSource _processing = new();
    private readonly CancellationToken _processingToken;
    private readonly TaskCompletionSource _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TimeProvider _timeProvider;
    private int _messageCount;

    public ConsumerSession(
        IMqttClient client,
        TimeProvider timeProvider,
        int index,
        Func<MqttApplicationMessageReceivedEventArgs, ConsumerSession, Task> onMessage
    )
    {
        _client = client;
        _timeProvider = timeProvider;
        Index = index;
        InFlight = new InFlightTracker(timeProvider);
        _processingToken = _processing.Token;

        _client.DisconnectedAsync += _ =>
        {
            End();

            return Task.CompletedTask;
        };
        _client.ApplicationMessageReceivedAsync += async arguments =>
        {
            await onMessage(arguments, this);
            Interlocked.Increment(ref _messageCount);
        };
    }

    public IMqttClient Client => _client;

    /// <summary>The index of the consumer, or of the orphan session, that owns the connection.</summary>
    public int Index { get; }

    public InFlightTracker InFlight { get; }

    public CancellationToken ProcessingToken => _processingToken;

    /// <summary>
    /// Completes when the connection dropped or a handler ended the session (a message stayed unacknowledged).
    /// </summary>
    public Task Ended => _ended.Task;

    public int MessageCount => Volatile.Read(ref _messageCount);

    public static void EnsureConnected(MqttClientConnectResult connectResult)
    {
        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            throw new InvalidOperationException(
                string.Format(MqttIntakeConstants.Messages.ConnectionRefused, connectResult.ResultCode));
        }
    }

    public void End() => _ended.TrySetResult();

    public async Task<MqttClientConnectResult> ConnectAsync(
        MqttClientOptions options,
        CancellationToken cancellationToken
    )
    {
        var connectResult = await _client.ConnectAsync(options, cancellationToken);

        EnsureConnected(connectResult);

        return connectResult;
    }

    public ProcessingTimeout CreateTimeout(TimeSpan delay, CancellationToken cancellationToken) =>
        new(delay, _timeProvider, cancellationToken, _processing.Cancel);

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(cancellationToken: CancellationToken.None);
            }
        }
        finally
        {
            _client.Dispose();
            _processing.Dispose();
        }
    }
}
