namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// Defines the exceptions that a received message throws when a caller breaks an invariant.
/// </summary>
public static class ReceivedMessageInvariantErrors
{
    public static InvalidOperationException ExactlyOnePayloadForm() =>
        new(message: "A received message holds exactly one of the JSON payload and the raw bytes.");
}
