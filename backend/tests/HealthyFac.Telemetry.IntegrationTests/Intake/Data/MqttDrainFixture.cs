using System.Diagnostics.Metrics;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Starts the same services as <see cref="MqttIntakeFixture"/> but starts no consumer. A stop or start test creates
/// its own consumers with <see cref="CreateIntake"/>, so it can stop and start them.
/// </summary>
public sealed class MqttDrainFixture : MqttIntakeFixture
{
    private static readonly TimeSpan ConnectLimit = TimeSpan.FromSeconds(seconds: 30);

    private readonly MqttClientFactory _factory = new();

    protected override bool IsIntakeStarted => false;

    public StartedIntake CreateIntake(
        string sharedGroup,
        string clientId,
        int? drainTimeoutSeconds = null,
        int consumerCount = 1
    )
    {
        var state = new MqttIntakeState();
        var defaults = new MqttIntakeOptions();
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
            new IntakeMetrics(MeterFactory, state),
            Database.GetSingleton<ILogger<MqttIntakeService>>());

        return new StartedIntake { Service = service, State = state };
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
        using var client = _factory.CreateMqttClient();
        await ReconnectAsync(clientId, client);
        await client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(
                    string.Format(MqttIntakeConstants.SharedSubscriptionFormat, sharedGroup),
                    MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());
        await client.DisconnectAsync();
    }

    public IMqttClient CreateClient() => _factory.CreateMqttClient();

    public static async Task WaitUntilConnectedAsync(StartedIntake intake)
    {
        var deadline = DateTime.UtcNow + ConnectLimit;

        while (!intake.State.IsConnected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100));
        }
    }
}
