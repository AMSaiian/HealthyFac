namespace HealthyFac.Core.Application.MachineErrorCodes;

/// <summary>
/// Defines validation length constants for machine error code properties.
/// </summary>
public static class MachineErrorCodeValidationConstants
{
    public const int CodeMaxLength = 50;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;
}
