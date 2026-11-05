namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines validation length constants for machine properties.
/// </summary>
public static class MachineValidationConstants
{
    public const int NameMaxLength = 200;
    public const int SerialNumberMaxLength = 100;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;
}
