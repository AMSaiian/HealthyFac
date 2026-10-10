namespace HealthyFac.Shared.Web.Tracing;

/// <summary>
/// Defines the validator messages for the tracing configuration.
/// </summary>
public static class TracingValidationMessages
{
    public const string SamplingRatioOutOfRange = "The sampling ratio must be between 0 and 1.";
    public const string SourceNameRequired = "Each additional source name must hold text.";
}
