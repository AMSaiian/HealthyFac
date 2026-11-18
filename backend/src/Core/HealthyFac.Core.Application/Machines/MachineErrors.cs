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
    public const string StateRequired = "The state is required.";
    public const string MachineNotFound = "The machine does not exist.";
    public const string SerialNumberTaken = "A machine with this serial number already exists.";
    public const string MachineChanged = "The machine changed. Reload it and try again.";
    public const string MachineIdsRequired = "The list of machine IDs is required.";
    public const string MachineDraftIdsRequired = "The list of machine draft IDs is required.";

    public static readonly string NameLookupTooManyIds =
        $"A name lookup accepts at most {MachineValidationConstants.NameLookupMaxIds} IDs of one kind.";

    public const string ExpectedGoLiveOnBeforeToday =
        "The expected go-live date must be today or later in the company time zone.";

    public const string ExpectedGoLiveOnBeforeCreation =
        "The expected go-live date must not be before the creation day of the machine.";

    public const string ExpectedGoLiveOnOnUpdateDraft =
        "An update draft keeps the expected go-live date of the machine.";

    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {MachineValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
