namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// Defines the constants of the machine message intake: the topic pattern, the clock tolerance, and the limits.
/// </summary>
public static class IntakeConstants
{
    public const string TopicPrefix = "healthyfac/gateways/";
    public const string TopicSuffix = "/messages";
    public const int TopicMaxLength = 1024;

    /// <summary>The longest time that <c>sampledAt</c> may lie ahead of the receipt time. It covers a small clock
    /// difference between the machine and the server.</summary>
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(minutes: 1);

    /// <summary>The name of the unique index on the machine and the sampling time of the received messages. The
    /// mapping in Infrastructure reads this name.</summary>
    public const string DuplicateIndexName = "ix_received_messages_machine_registration_id_sampled_at";
}
