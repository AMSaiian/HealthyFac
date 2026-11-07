using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a Mosquitto container for integration tests, with the image from <see cref="TestImages"/> and the access
/// rules of the machine broker: no anonymous access, user <see cref="TelemetryUser"/> subscribes to the gateway
/// topics, and user <see cref="SenderUser"/> publishes to them. <see cref="StopAsync"/> and
/// <see cref="StartAsync"/> let a test simulate a broker outage.
/// </summary>
public sealed class MosquittoTestBroker : IAsyncDisposable
{
    public const string TelemetryUser = "telemetry";
    public const string SenderUser = "sender";

    private const ushort BrokerContainerPort = 1883;
    private const string ConfigDirectory = "/mosquitto/config";
    private const string ConfigPath = $"{ConfigDirectory}/mosquitto.conf";
    private const string AclPath = $"{ConfigDirectory}/acl";
    private const string PasswordPath = $"{ConfigDirectory}/passwords";
    private const string MaxPacketSize = "65536";
    private const string TopicFilter = "healthyfac/gateways/+/messages";
    private const string ReadyMessagePattern = "mosquitto version .* running";

    private static readonly string BrokerConfig = $"""
                                                   listener {BrokerContainerPort}
                                                   allow_anonymous false
                                                   password_file {PasswordPath}
                                                   acl_file {AclPath}
                                                   max_packet_size {MaxPacketSize}
                                                   """;

    private static readonly string AccessRules = $"""
                                                  user {TelemetryUser}
                                                  topic read {TopicFilter}

                                                  user {SenderUser}
                                                  topic write {TopicFilter}
                                                  """;

    private readonly IContainer _container;

    public MosquittoTestBroker()
    {
        TelemetryPassword = Guid.NewGuid().ToString(format: "N");
        SenderPassword = Guid.NewGuid().ToString(format: "N");

        var startScript =
            $"mosquitto_passwd -b -c {PasswordPath} {TelemetryUser} {TelemetryPassword}"
            + $" && mosquitto_passwd -b {PasswordPath} {SenderUser} {SenderPassword}"
            + $" && chown mosquitto:mosquitto {PasswordPath}"
            + $" && exec mosquitto -c {ConfigPath}";

        _container = new ContainerBuilder(TestImages.Mosquitto)
            .WithPortBinding(BrokerContainerPort, assignRandomHostPort: true)
            .WithResourceMapping(Encoding.UTF8.GetBytes(BrokerConfig), ConfigPath)
            .WithResourceMapping(Encoding.UTF8.GetBytes(AccessRules), AclPath)
            .WithEntrypoint("sh", "-c")
            .WithCommand(startScript)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(ReadyMessagePattern))
            .Build();
    }

    public string TelemetryPassword { get; }

    public string SenderPassword { get; }

    /// <summary>
    /// The host and the mapped port, read once at start. They stay available after <see cref="StopAsync"/>, because
    /// the container then drops its port mapping, and a test needs the address of the stopped broker.
    /// </summary>
    public string Host { get; private set; } = string.Empty;

    public int Port { get; private set; }

    public async Task StartAsync()
    {
        await _container.StartAsync();

        Host = _container.Hostname;
        Port = _container.GetMappedPublicPort(BrokerContainerPort);
    }

    public Task StopAsync() => _container.StopAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
