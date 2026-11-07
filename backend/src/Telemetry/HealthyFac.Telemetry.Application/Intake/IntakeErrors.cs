namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// Defines error messages for the intake use case.
/// </summary>
public static class IntakeErrors
{
    public const string TopicRequired = "Topic is required.";
    public const string PayloadRequired = "Payload is required.";
    public const string ReceivedAtRequired = "ReceivedAt is required.";
}
