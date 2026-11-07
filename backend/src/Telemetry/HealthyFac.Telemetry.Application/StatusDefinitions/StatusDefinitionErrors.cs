using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

/// <summary>
/// Defines error messages for status definition operations.
/// </summary>
public static class StatusDefinitionErrors
{
    public const string CodeRequired = "Status code is required.";
    public const string SeverityRequired = "Severity is required.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string StatusDefinitionIdRequired = "Status definition ID is required.";
    public const string StatusDefinitionNotFound = "The status definition does not exist.";
    public const string CodeTaken = "A status definition with this code already exists for this machine.";
    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {StatusDefinitionValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
