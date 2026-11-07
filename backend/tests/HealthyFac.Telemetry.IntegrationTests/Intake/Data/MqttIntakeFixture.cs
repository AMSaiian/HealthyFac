using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Starts PostgreSQL, Mosquitto, and the Telemetry services with the MQTT consumer turned on. A test starts the
/// consumer itself.
/// </summary>
public class MqttIntakeFixture : IAsyncLifetime
{
    private static readonly TimeSpan ConnectLimit = TimeSpan.FromSeconds(seconds: 30);

    private readonly MosquittoTestBroker _broker = new();
    private readonly MqttClientFactory _clientFactory = new();

    private TelemetryDatabaseFixture? _database;

    public FailFirstSaveInterceptor SaveFailure { get; } = new();

    public OverlapProbeInterceptor OverlapProbe { get; } = new();

    public SlowSaveInterceptor SlowSave { get; } = new();

    public TelemetryDatabaseFixture Database => _database!;

    public MqttIntakeService Intake => Database.GetHostedService<MqttIntakeService>();

    public MqttIntakeState State => Database.GetSingleton<MqttIntakeState>();

    public MosquittoTestBroker Broker => _broker;

    protected virtual int ConsumerCount => 1;

    protected virtual bool IsIntakeStarted => true;

    public async Task InitializeAsync()
    {
        await _broker.StartAsync();

        _database = new TelemetryDatabaseFixture(
            settings =>
            {
                settings[key: "MqttIntake:IsEnabled"] = bool.TrueString;
                settings[key: "MqttIntake:Host"] = _broker.Host;
                settings[key: "MqttIntake:Port"] = _broker.Port.ToString();
                settings[key: "MqttIntake:Username"] = MosquittoTestBroker.TelemetryUser;
                settings[key: "MqttIntake:Password"] = _broker.TelemetryPassword;
                settings[key: "MqttIntake:ClientId"] = "intake-" + Guid.NewGuid().ToString(format: "N");
                settings[key: "MqttIntake:SharedGroup"] = Guid.NewGuid().ToString(format: "N");
                settings[key: "MqttIntake:ReconnectDelaySeconds"] = "1";
                settings[key: "MqttIntake:ConsumerCount"] = ConsumerCount.ToString();
            },
            services =>
                services.ConfigureDbContext<HealthyFac.Telemetry.Infrastructure.Database.TelemetryDbContext>(options =>
                    options.AddInterceptors(SaveFailure, OverlapProbe, SlowSave)));

        await _database.InitializeAsync();

        if (!IsIntakeStarted)
        {
            return;
        }

        await Intake.StartAsync(CancellationToken.None);

        var deadline = DateTime.UtcNow + ConnectLimit;

        while (State.ConnectedCount < ConsumerCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100));
        }
    }

    public async Task DisposeAsync()
    {
        await Intake.StopAsync(CancellationToken.None);
        await _database!.DisposeAsync();
        await _broker.DisposeAsync();
    }

    public async Task PublishAsync(string topic, byte[] payload)
    {
        using var sender = _clientFactory.CreateMqttClient();
        await sender.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithTcpServer(_broker.Host, _broker.Port)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithCredentials(MosquittoTestBroker.SenderUser, _broker.SenderPassword)
                .Build());

        await sender.PublishAsync(
            new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
        await sender.DisconnectAsync();
    }
}
