using HealthyFac.Telemetry.Application.Intake;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Persistence;

/// <summary>
/// Defines the limits and the names of the received message table that code reads.
/// </summary>
public static class ReceivedMessageConstants
{
    /// <summary>The longest topic that the database accepts. A topic is at most 65,535 bytes in MQTT, and the broker
    /// limits a packet far below it.</summary>
    public const int TopicMaxLength = 1024;

    public const string ExactlyOnePayloadFormCheck = "(payload IS NULL) <> (raw_payload IS NULL)";

    public const string ExactlyOnePayloadFormConstraintName = "ck_received_message_exactly_one_payload_form";

    public const string UniqueFilter = "machine_registration_id IS NOT NULL AND sampled_at IS NOT NULL";

    public static class IndexNames
    {
        public const string MachineSampledAt = IntakeConstants.DuplicateIndexName;
    }
}
