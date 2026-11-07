using FluentValidation;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Configures the MQTT consumer of machine messages. The consumer is off until a host turns it on.
/// </summary>
public sealed class MqttIntakeOptions
{
    public const string SectionName = "MqttIntake";

    public bool IsEnabled { get; init; }

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 1883;

    public string Username { get; init; } = "telemetry";

    public string Password { get; init; } = string.Empty;

    /// <summary>The base of the MQTT client IDs of this instance. It must be fixed and unique per instance, because the
    /// broker keeps each session, and its unacknowledged messages, by the client ID.</summary>
    public string ClientId { get; init; } = "telemetry";

    /// <summary>The number of MQTT consumers in this instance. Consumer <c>i</c> connects with the client ID
    /// <c>{ClientId}-{i}</c>, so the number must stay the same between restarts to keep each session.</summary>
    public int ConsumerCount { get; init; } = 1;

    /// <summary>The name of the shared subscription group. All instances of the service use the same group, so the
    /// broker gives each message to one of them.</summary>
    public string SharedGroup { get; init; } = "telemetry";

    /// <summary>The time that the broker keeps the session of an offline instance. Within this time the instance gets
    /// the messages that it did not acknowledge.</summary>
    public int SessionExpirySeconds { get; init; } = 3600;

    public int ReconnectDelaySeconds { get; init; } = 5;

    /// <summary>The longest time that a stopping consumer waits to finish its messages. It must stay below the stop
    /// time of the host (<c>HostOptions.ShutdownTimeout</c>, 30 seconds by default) and the <c>stop_grace_period</c> of
    /// Compose (30 seconds), or the process ends before the consumer deletes its session.</summary>
    public int DrainTimeoutSeconds { get; init; } = 20;

    /// <summary>The time without a received message that a stopping consumer waits for, after it has unsubscribed,
    /// before it treats the messages that the broker assigned to it as finished.</summary>
    public int DrainIdleSeconds { get; init; } = 2;
}

/// <summary>
/// Validates the MQTT consumer settings at startup. A disabled consumer needs no setting.
/// </summary>
public sealed class MqttIntakeOptionsValidator : AbstractValidator<MqttIntakeOptions>
{
    private static readonly char[] ForbiddenGroupCharacters = ['/', '+', '#'];

    public MqttIntakeOptionsValidator()
    {
        When(
            options => options.IsEnabled, () =>
            {
                RuleFor(options => options.Host)
                    .NotEmpty()
                    .WithMessage(MqttIntakeConstants.Messages.HostRequired);

                RuleFor(options => options.Port)
                    .InclusiveBetween(from: 1, MqttIntakeConstants.Limits.MaxPort)
                    .WithMessage(MqttIntakeConstants.Messages.PortOutOfRange);

                RuleFor(options => options.Username)
                    .NotEmpty()
                    .WithMessage(MqttIntakeConstants.Messages.UsernameRequired);

                RuleFor(options => options.Password)
                    .NotEmpty()
                    .WithMessage(MqttIntakeConstants.Messages.PasswordRequired);

                RuleFor(options => options.ClientId)
                    .NotEmpty()
                    .WithMessage(MqttIntakeConstants.Messages.ClientIdInvalid)
                    .Must((options, clientId) => string.Format(
                                MqttIntakeConstants.ConsumerClientIdFormat,
                                clientId,
                                Math.Max(options.ConsumerCount - 1, val2: 0))
                            .Length
                        <= MqttIntakeConstants.Limits.MaxClientIdLength)
                    .WithMessage(MqttIntakeConstants.Messages.ClientIdInvalid);

                RuleFor(options => options.ConsumerCount)
                    .InclusiveBetween(from: 1, MqttIntakeConstants.Limits.MaxConsumerCount)
                    .WithMessage(MqttIntakeConstants.Messages.ConsumerCountOutOfRange);

                RuleFor(options => options.SharedGroup)
                    .NotEmpty()
                    .WithMessage(MqttIntakeConstants.Messages.SharedGroupInvalid)
                    .Must(group => group.IndexOfAny(ForbiddenGroupCharacters) < 0)
                    .WithMessage(MqttIntakeConstants.Messages.SharedGroupInvalid);

                RuleFor(options => options.SessionExpirySeconds)
                    .InclusiveBetween(
                        MqttIntakeConstants.Limits.MinSessionExpirySeconds,
                        MqttIntakeConstants.Limits.MaxSessionExpirySeconds)
                    .WithMessage(MqttIntakeConstants.Messages.SessionExpiryOutOfRange);

                RuleFor(options => options.DrainTimeoutSeconds)
                    .InclusiveBetween(
                        MqttIntakeConstants.Limits.MinDrainTimeoutSeconds,
                        MqttIntakeConstants.Limits.MaxDrainTimeoutSeconds)
                    .WithMessage(MqttIntakeConstants.Messages.DrainTimeoutOutOfRange);

                RuleFor(options => options.DrainIdleSeconds)
                    .InclusiveBetween(
                        MqttIntakeConstants.Limits.MinDrainIdleSeconds,
                        MqttIntakeConstants.Limits.MaxDrainIdleSeconds)
                    .WithMessage(MqttIntakeConstants.Messages.DrainIdleOutOfRange);

                RuleFor(options => options.ReconnectDelaySeconds)
                    .InclusiveBetween(
                        MqttIntakeConstants.Limits.MinReconnectDelaySeconds,
                        MqttIntakeConstants.Limits.MaxReconnectDelaySeconds)
                    .WithMessage(MqttIntakeConstants.Messages.ReconnectDelayOutOfRange);
            });
    }
}
