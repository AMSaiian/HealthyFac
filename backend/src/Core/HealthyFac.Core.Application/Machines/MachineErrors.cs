using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines error messages for machine operations.
/// </summary>
public static class MachineErrors
{
    public const string NameRequired = "Machine name is required.";
    public const string SerialNumberRequired = "Serial number is required.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string DepartmentIdRequired = "DepartmentId is required.";
    public const string DepartmentNotFound = "The department does not exist in the company.";
    public const string MachineNotFound = "The machine does not exist.";
    public const string SerialNumberTaken = "A machine with this serial number already exists.";
    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {MachineValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
