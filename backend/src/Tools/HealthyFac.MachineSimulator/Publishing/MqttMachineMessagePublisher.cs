using System.Buffers;
using System.Diagnostics;
using System.Text;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Telemetry.Application.Intake;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Publishes the machine messages with one MQTT client (the sender user) and keeps the connection. While the client is
/// disconnected, a publish returns <see langword="false"/> at once. A background loop connects again after the
/// reconnect delay.
/// </summary>
/// <param name="options">The connection settings.</param>
/// <param name="timeProvider">Times the reconnect loop.</param>
/// <param name="logger">Logs the connection changes. It never logs the password.</param>
public sealed class MqttMachineMessagePublisher(
    IOptions<SimulatorMqttOptions> options,
    TimeProvider timeProvider,
    ILogger<MqttMachineMessagePublisher> logger
) : BackgroundService, IMachineMessagePublisher
{
    private static readonly ActivitySource Source = new(ObservabilityConstants.Simulator.Source);

    private readonly SimulatorMqttOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<MqttMachineMessagePublisher> _logger = logger;
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();
    private bool _hasConnected;

    public bool IsConnected => _client.IsConnected;

    public async Task<bool> TryPublishAsync(
        string gatewayId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken
    )
    {
        if (!_client.IsConnected)
        {
            return false;
        }

        var topic = $"{IntakeConstants.TopicPrefix}{gatewayId}{IntakeConstants.TopicSuffix}";
        using var activity = Source.StartActivity(
            ObservabilityConstants.Simulator.PublishSpanName,
            ActivityKind.Producer);
        activity?.SetTag(ObservabilityConstants.Messaging.Tags.SystemName, ObservabilityConstants.Intake.MqttSystem);
        activity?.SetTag(ObservabilityConstants.Messaging.Tags.DestinationName, topic);

        var builder = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(new ReadOnlySequence<byte>(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(value: false);
        if (activity?.Id is { } traceParent)
        {
            builder.WithUserProperty(MessageHeaders.TraceParent, Encoding.UTF8.GetBytes(traceParent));
        }

        try
        {
            var result = await _client.PublishAsync(builder.Build(), cancellationToken);

            return result.IsSuccess;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(exception, message: "The publish to the broker failed");
            activity?.SetStatus(ActivityStatusCode.Error);

            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.DisconnectedAsync += args =>
        {
            if (args.ClientWasConnected && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(message: "The MQTT connection to the broker closed");
            }

            return Task.CompletedTask;
        };

        using var timer = new PeriodicTimer(_options.ReconnectDelay, _timeProvider);
        do
        {
            await ConnectIfNeededAsync(stoppingToken);
        }
        while (await WaitForNextAsync(timer, stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(cancellationToken: cancellationToken);
        }

        _client.Dispose();
    }

    private static async Task<bool> WaitForNextAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task ConnectIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
        {
            return;
        }

        try
        {
            await _client.ConnectAsync(BuildClientOptions(), cancellationToken);
            _logger.LogInformation(
                _hasConnected
                    ? "Reconnected to the MQTT broker {Host}:{Port}"
                    : "Connected to the MQTT broker {Host}:{Port}",
                _options.Host,
                _options.Port);
            _hasConnected = true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(exception, message: "The connection to the MQTT broker failed");
        }
    }

    private MqttClientOptions BuildClientOptions() =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(_options.ClientId)
            .WithCredentials(_options.Username, _options.Password)
            .Build();
}
