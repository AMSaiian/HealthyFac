using System.Diagnostics.Metrics;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Resetting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Starts PostgreSQL, Mosquitto, and the Telemetry services with the MQTT consumer registered but not started. A test
/// creates its own consumers with <see cref="CreateIntake"/>, so it chooses the consumer count and the drain time and
/// can stop and start them. Each consumer has its own meter factory, so a test reads only its own measurements. Each
/// test uses its own shared subscription group and client IDs, so no session that an earlier test left on the broker
/// delivers a message to a later test.
/// </summary>
public sealed class MqttIntakeFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private static readonly TimeSpan ConnectLimit = TimeSpan.FromSeconds(seconds: 30);

    private readonly MosquittoTestBroker _broker = new();
    private readonly MqttClientFactory _clientFactory = new();

    private TelemetryDatabaseFixture? _database;

    public MqttIntakeFixture()
    {
        Mosquitto = new MosquittoFixture(_broker);
    }

    public MosquittoFixture Mosquitto { get; }

    public FailFirstSaveInterceptor SaveFailure { get; } = new();

    public OverlapProbeInterceptor OverlapProbe { get; } = new();

    public SlowSaveInterceptor SlowSave { get; } = new();

    public TelemetryDatabaseFixture Database => _database!;

    public MosquittoTestBroker Broker => _broker;

    public async ValueTask InitializeAsync()
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
            },
            services =>
                services.ConfigureDbContext<HealthyFac.Telemetry.Infrastructure.Database.TelemetryDbContext>(options =>
                    options.AddInterceptors(SaveFailure, OverlapProbe, SlowSave)));

        await _database.InitializeAsync();
    }

    /// <summary>
    /// Deletes the rows and disarms the save interceptors. The broker keeps the sessions of earlier tests; every test
    /// uses new group and client names, so they do not matter.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await Database.ResetAsync();
        SaveFailure.Reset();
        OverlapProbe.Reset();
        SlowSave.Reset();
    }

    public async ValueTask DisposeAsync()
    {
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

    public StartedIntake CreateIntake(
        string sharedGroup,
        string clientId,
        int? drainTimeoutSeconds = null,
        int consumerCount = 1
    )
    {
        var state = new MqttIntakeState();
        var defaults = new MqttIntakeOptions();
        var meterServices = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var service = new MqttIntakeService(
            Options.Create(
                new MqttIntakeOptions
                {
                    IsEnabled = true,
                    Host = Broker.Host,
                    Port = Broker.Port,
                    Username = MosquittoTestBroker.TelemetryUser,
                    Password = Broker.TelemetryPassword,
                    ClientId = clientId,
                    SharedGroup = sharedGroup,
                    ConsumerCount = consumerCount,
                    ReconnectDelaySeconds = 1,
                    DrainTimeoutSeconds = drainTimeoutSeconds ?? defaults.DrainTimeoutSeconds,
                    DrainIdleSeconds = 1,
                }),
            Database.GetSingleton<IServiceScopeFactory>(),
            Database.GetSingleton<TimeProvider>(),
            state,
            new IntakeMetrics(meterServices.GetRequiredService<IMeterFactory>(), state),
            Database.GetSingleton<ILogger<MqttIntakeService>>());

        return new StartedIntake { Service = service, State = state, MeterServices = meterServices };
    }

    public async Task<MqttClientConnectResult> ReconnectAsync(string clientId, IMqttClient client)
    {
        var result = await client.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithTcpServer(Broker.Host, Broker.Port)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithClientId(clientId)
                .WithCredentials(MosquittoTestBroker.TelemetryUser, Broker.TelemetryPassword)
                .WithCleanStart(value: false)
                .WithSessionExpiryInterval(sessionExpiryInterval: 60)
                .Build());

        return result;
    }

    /// <summary>
    /// Creates a session in the shared group, as an earlier run with a higher consumer count would have left it.
    /// </summary>
    public async Task CreateSessionAsync(string clientId, string sharedGroup)
    {
        using var client = _clientFactory.CreateMqttClient();
        await ReconnectAsync(clientId, client);
        await client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(
                    string.Format(MqttIntakeConstants.SharedSubscriptionFormat, sharedGroup),
                    MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
        await client.DisconnectAsync();
    }

    public IMqttClient CreateClient() => _clientFactory.CreateMqttClient();

    public static async Task WaitUntilConnectedAsync(StartedIntake intake, int consumerCount = 1)
    {
        var deadline = DateTime.UtcNow + ConnectLimit;

        while (intake.State.ConnectedCount < consumerCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100));
        }
    }
}
