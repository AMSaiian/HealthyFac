using FluentValidation;

namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Configures the MQTT connection of the machine simulator. The user is the sender user of the broker.
/// </summary>
public sealed class SimulatorMqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 1883;

    public string Username { get; init; } = "sender";

    public string Password { get; init; } = string.Empty;

    public string ClientId { get; init; } = "machine-simulator";

    /// <summary>The time between two connect attempts while the broker is not reachable.</summary>
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(seconds: 5);
}

public sealed class SimulatorMqttOptionsValidator : AbstractValidator<SimulatorMqttOptions>
{
    public const int MaxPort = 65535;
    public const string HostMessage = "The MQTT host is required.";
    public const string PortMessage = "The MQTT port must be from 1 to 65535.";
    public const string UsernameMessage = "The MQTT user name is required.";
    public const string PasswordMessage = "The MQTT password is required.";
    public const string ClientIdMessage = "The MQTT client ID is required.";
    public const string ReconnectMessage = "The MQTT reconnect delay must be above 0 seconds.";

    public SimulatorMqttOptionsValidator()
    {
        RuleFor(options => options.Host).NotEmpty().WithMessage(HostMessage);
        RuleFor(options => options.Port).InclusiveBetween(from: 1, MaxPort).WithMessage(PortMessage);
        RuleFor(options => options.Username).NotEmpty().WithMessage(UsernameMessage);
        RuleFor(options => options.Password).NotEmpty().WithMessage(PasswordMessage);
        RuleFor(options => options.ClientId).NotEmpty().WithMessage(ClientIdMessage);
        RuleFor(options => options.ReconnectDelay).GreaterThan(TimeSpan.Zero).WithMessage(ReconnectMessage);
    }
}
