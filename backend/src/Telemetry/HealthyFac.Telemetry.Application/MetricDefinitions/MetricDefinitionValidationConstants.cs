namespace HealthyFac.Telemetry.Application.MetricDefinitions;

/// <summary>
/// Defines validation length constants for metric definition properties.
/// </summary>
public static class MetricDefinitionValidationConstants
{
    public const int NameMaxLength = 100;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;
}
