namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Defines the limits of the text that the signal messages carry. The values equal the Telemetry limits of a metric
/// key (<c>MetricDefinitionRules.KeyMaxLength</c>), a status code (<c>StatusDefinitionValidationConstants</c>) and a
/// phase code, which Messaging cannot reference.
/// </summary>
public static class SignalMessageLimits
{
    public const int KeyMaxLength = 100;
    public const int StatusCodeMaxLength = 50;
}
