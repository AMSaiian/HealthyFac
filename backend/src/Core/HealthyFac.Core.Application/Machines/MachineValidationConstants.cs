using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines validation length constants for machine properties.
/// </summary>
public static class MachineValidationConstants
{
    public const int NameMaxLength = MachineMessageLimits.NameMaxLength;
    public const int SerialNumberMaxLength = 100;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;

    /// <summary>
    /// The most IDs of one kind that a name lookup accepts. It equals the largest page of a grid, because the
    /// Gateway looks up the names of one page.
    /// </summary>
    public const int NameLookupMaxIds = GridValidationConstants.MaxLimit;
}
