namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Defines the constants of the MQTT consumer.
/// </summary>
public static class MqttIntakeConstants
{
    public const string SharedSubscriptionFormat = "$share/{0}/healthyfac/gateways/+/messages";
    public const string HealthCheckName = "mqtt-intake";
    public const string ConsumerClientIdFormat = "{0}-{1}";
    public const int DrainPollMilliseconds = 100;

    public static class Limits
    {
        public const int MaxPort = 65535;
        public const int MinReconnectDelaySeconds = 1;
        public const int MaxReconnectDelaySeconds = 300;
        public const int MinSessionExpirySeconds = 60;
        public const int MaxSessionExpirySeconds = 86400;
        public const int MaxClientIdLength = 256;
        public const int MaxConsumerCount = 16;
        public const int StopSeconds = 30;
        public const int MinDrainTimeoutSeconds = 1;
        public const int MaxDrainTimeoutSeconds = StopSeconds - 1;
        public const int MinDrainIdleSeconds = 1;
        public const int MaxDrainIdleSeconds = 10;
    }

    public static class Messages
    {
        public const string HostRequired = "MqttIntake:Host is required.";
        public static readonly string PortOutOfRange = $"MqttIntake:Port must be between 1 and {Limits.MaxPort}.";
        public const string UsernameRequired = "MqttIntake:Username is required.";
        public const string PasswordRequired = "MqttIntake:Password is required.";

        public static readonly string ClientIdInvalid =
            "MqttIntake:ClientId is required. With the suffix '-' and the consumer index, it has at most "
            + $"{Limits.MaxClientIdLength} characters.";

        public static readonly string DrainTimeoutOutOfRange =
            "MqttIntake:DrainTimeoutSeconds must be between "
            + $"{Limits.MinDrainTimeoutSeconds} and {Limits.MaxDrainTimeoutSeconds}, "
            + $"below the {Limits.StopSeconds} second stop time.";

        public static readonly string DrainIdleOutOfRange =
            "MqttIntake:DrainIdleSeconds must be between "
            + $"{Limits.MinDrainIdleSeconds} and {Limits.MaxDrainIdleSeconds}.";

        public static readonly string ConsumerCountOutOfRange =
            $"MqttIntake:ConsumerCount must be between 1 and {Limits.MaxConsumerCount}.";

        public const string SharedGroupInvalid =
            "MqttIntake:SharedGroup is required and must not contain '/', '+', or '#'.";

        public static readonly string SessionExpiryOutOfRange =
            "MqttIntake:SessionExpirySeconds must be between "
            + $"{Limits.MinSessionExpirySeconds} and {Limits.MaxSessionExpirySeconds}.";

        public static readonly string ReconnectDelayOutOfRange =
            "MqttIntake:ReconnectDelaySeconds must be between "
            + $"{Limits.MinReconnectDelaySeconds} and {Limits.MaxReconnectDelaySeconds}.";

        public const string ConnectionRefused = "The MQTT broker refused the connection: {0}";

        public const string OrphanSessionInterrupted =
            "The orphan session {0} ended before its messages were finished. It stays for the next start.";
    }
}
