using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Core.Application.MachineErrorCodes;

/// <summary>
/// Defines error messages for machine error code operations.
/// </summary>
public static class MachineErrorCodeErrors
{
    public const string CodeRequired = "Error code is required.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string ErrorCodeIdRequired = "Error code ID is required.";
    public const string ErrorCodeNotFound = "The error code does not exist.";
    public const string CodeTaken = "An error code with this code already exists for this machine.";
    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {MachineErrorCodeValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
