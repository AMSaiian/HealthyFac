using HealthyFac.Testing.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Creates MQTT 5 clients for the Mosquitto test broker that the owner started. The owner disposes the broker.
/// </summary>
public sealed class MosquittoFixture(MosquittoTestBroker broker)
{
    private readonly MqttClientFactory _factory = new();
    private readonly MosquittoTestBroker _broker = broker;

    public MosquittoTestBroker Broker => _broker;

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

    /// <summary>
    /// Connects a client that the caller created and gave its message handler first. On a connect to a kept session,
    /// the broker sends the held and queued messages right after the connect acknowledgement. A client without a
    /// handler at that moment acknowledges and drops them, so the test never sees them.
    /// </summary>
    /// <param name="client">The client from <see cref="CreateClient"/>, with its message handler attached.</param>
    /// <param name="clientId">The client ID of the session.</param>
    /// <param name="sessionExpirySeconds">The session expiry interval that the connect sends.</param>
    /// <returns>The connect result.</returns>
    public Task<MqttClientConnectResult> ConnectTelemetryToSessionAsync(
        IMqttClient client,
        string clientId,
        uint sessionExpirySeconds
    ) =>
        client.ConnectAsync(
            CreateOptions(
                MosquittoTestBroker.TelemetryUser,
                _broker.TelemetryPassword,
                clientId,
                isCleanStart: false,
                sessionExpirySeconds));

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
