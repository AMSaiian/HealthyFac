using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using HealthyFac.Testing.Containers;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

/// <summary>
/// Subscribes to the gateway topics as the telemetry user and keeps every message, so a test reads what the simulator
/// sent through the real broker.
/// </summary>
public sealed class MachineMessageSubscriber : IAsyncDisposable
{
    private readonly ConcurrentQueue<ReceivedMachineMessage> _messages = new();
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();

    public IReadOnlyList<ReceivedMachineMessage> Messages => [.. _messages];

    public async Task ConnectAsync(MosquittoTestBroker broker)
    {
        _client.ApplicationMessageReceivedAsync += args =>
        {
            var message = args.ApplicationMessage;

            _messages.Enqueue(
                new ReceivedMachineMessage(
                    message.Topic,
                    message.Payload.ToArray(),
                    (message.UserProperties ?? [])
                    .ToDictionary(property => property.Name, property => property.ReadValueAsString())));

            return Task.CompletedTask;
        };

        await _client.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithTcpServer(broker.Host, broker.Port)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithClientId($"subscriber-{Guid.NewGuid():N}")
                .WithCredentials(MosquittoTestBroker.TelemetryUser, broker.TelemetryPassword)
                .Build());

        await _client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(SimulatorTestData.TopicFilter, MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
    }

    public void Clear() => _messages.Clear();

    public async Task<IReadOnlyList<ReceivedMachineMessage>> WaitForAsync(int count, TimeSpan limit)
    {
        var deadline = DateTime.UtcNow + limit;

        while (_messages.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 25));
        }

        return Messages;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }

        _client.Dispose();
    }
}

public sealed record ReceivedMachineMessage(
    string Topic,
    byte[] Payload,
    IReadOnlyDictionary<string, string> UserProperties
)
{
    public string Text => Encoding.UTF8.GetString(Payload);
}
