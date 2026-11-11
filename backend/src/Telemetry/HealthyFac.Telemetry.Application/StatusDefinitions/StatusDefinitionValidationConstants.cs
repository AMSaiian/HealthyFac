namespace HealthyFac.Telemetry.Application.StatusDefinitions;

/// <summary>
/// Defines validation length constants for status definition properties.
/// </summary>
public static class StatusDefinitionValidationConstants
{
    public const int CodeMaxLength = 50;

    /// <summary>The most items that one batch of status definitions holds.</summary>
    public const int ApplyMaxItems = 200;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;
}
