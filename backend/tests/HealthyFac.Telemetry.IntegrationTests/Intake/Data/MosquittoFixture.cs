using HealthyFac.Testing.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Starts the Mosquitto test broker once for all tests of a class and creates MQTT 5 clients for it.
/// </summary>
public sealed class MosquittoFixture : IAsyncLifetime
{
    private readonly MqttClientFactory _factory = new();
    private readonly MosquittoTestBroker _broker = new();

    public MosquittoTestBroker Broker => _broker;

    public Task InitializeAsync() => _broker.StartAsync();

    public Task DisposeAsync() => _broker.DisposeAsync().AsTask();

    public MqttClientOptions CreateOptions(
        string? user,
        string? password,
        string? clientId = null,
        bool isCleanStart = true,
        uint sessionExpirySeconds = 0,
        ushort receiveMaximum = 0
    )
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker.Host, _broker.Port)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithClientId(clientId ?? Guid.NewGuid().ToString(format: "N"))
            .WithCleanStart(isCleanStart)
            .WithSessionExpiryInterval(sessionExpirySeconds);

        if (user is not null)
        {
            builder.WithCredentials(user, password);
        }

        if (receiveMaximum > 0)
        {
            builder.WithReceiveMaximum(receiveMaximum);
        }

        return builder.Build();
    }

    public async Task<(IMqttClient Client, MqttClientConnectResult Result)> ConnectTelemetryWithResultAsync(
        string clientId,
        uint sessionExpirySeconds,
        ushort receiveMaximum = 0
    )
    {
        var client = _factory.CreateMqttClient();
        var result = await client.ConnectAsync(
            CreateOptions(
                MosquittoTestBroker.TelemetryUser,
                _broker.TelemetryPassword,
                clientId,
                isCleanStart: false,
                sessionExpirySeconds,
                receiveMaximum));

        return (client, result);
    }

    public async Task<IMqttClient> ConnectTelemetryAsync(
        string? clientId = null,
        bool isCleanStart = true,
        uint sessionExpirySeconds = 0
    )
    {
        var client = _factory.CreateMqttClient();
        await client.ConnectAsync(
            CreateOptions(
                MosquittoTestBroker.TelemetryUser,
                _broker.TelemetryPassword,
                clientId,
                isCleanStart,
                sessionExpirySeconds));

        return client;
    }

    public async Task<IMqttClient> ConnectSenderAsync()
    {
        var client = _factory.CreateMqttClient();
        await client.ConnectAsync(CreateOptions(MosquittoTestBroker.SenderUser, _broker.SenderPassword));

        return client;
    }

    public IMqttClient CreateClient() => _factory.CreateMqttClient();

    public static Task<MqttClientPublishResult> PublishAsync(IMqttClient client, string topic, string payload) =>
        client.PublishAsync(
            new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
}
