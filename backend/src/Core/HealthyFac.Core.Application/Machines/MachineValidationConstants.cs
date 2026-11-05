namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines validation length constants for machine properties.
/// </summary>
public static class MachineValidationConstants
{
    public const int NameMaxLength = 200;
    public const int SerialNumberMaxLength = 100;

    /// <summary>The limit counts the characters of the stored HTML string, not the visible text.</summary>
    public const int DescriptionMaxLength = 5000;
}
