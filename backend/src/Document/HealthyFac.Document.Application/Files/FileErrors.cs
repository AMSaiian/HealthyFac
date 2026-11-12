using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Document.Application.Files;

/// <summary>
/// Defines the error messages of the file use cases.
/// </summary>
public static class FileErrors
{
    /// <summary>
    /// The stable key that the web app maps to a translated text. The upload returns it under the key <c>file</c> when
    /// the content, the extension, and the declared type do not agree on an accepted type.
    /// </summary>
    public const string TypeNotAllowed = "fileTypeNotAllowed";

    public const string FileRequired = "A file is required.";
    public const string InvalidText = "The text is not valid Unicode.";
    public const string ExactlyOneFile = "The request must hold exactly one file.";
    public const string FileEmpty = "The file must have at least 1 byte.";
    public const string NameRequired = "The file name must not be empty after the extension is removed.";
    public const string DeclaredTypeTooLong = "The declared content type is too long.";
    public const string FileNotFound = "The file does not exist.";
    public const string FileChanged = "The file changed since you read it. Reload it and try again.";
    public const string FileIdRequired = "The file is required.";
    public const string LinkIdsRequired = "The list of file IDs is required.";

    public static readonly string LinkIdsTooMany =
        $"The request must hold at most {FileValidationConstants.LinkIdsMaxCount} file IDs.";

    public const string LinkAudienceInvalid = "The link audience is not valid.";

    public static readonly string LookupIdsTooMany =
        $"The request must hold at most {FileValidationConstants.LookupIdsMaxCount} file IDs.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {FileValidationConstants.Text.DescriptionMaxLength} characters.";

    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
    public const string CompanyIdRequired = "The company is required.";
    public const string MachineIdRequired = "The machine is required.";
    public const string MachineFilterIdsRequired = "The machine filter needs its machine IDs.";
    public const string MachineFilterEmpty = "The machine filter needs a machine or the option for untied files.";
    public const string IngestStateFilterStatesRequired = "The ingest state filter needs its states.";
    public const string IngestStateUnknown = "The ingest state filter holds a state that does not exist.";

    public const string IngestStateFilterEmpty =
        "The ingest state filter needs a state or the option for never ingested.";

    public static readonly string NameTooLong =
        $"The file name must have at most {FileValidationConstants.Text.FileNameMaxLength} characters.";

    public static readonly string AttachmentTooLarge =
        $"The file must have at most {FileValidationConstants.Size.AttachmentMaxBytes} bytes.";

    public static readonly string EmbeddedTooLarge =
        $"The image must have at most {FileValidationConstants.Size.EmbeddedMaxBytes} bytes.";
}
