namespace HealthyFac.Document.Application.Files;

/// <summary>
/// Defines the limits of file input. The frontend mirrors them in <c>files-limits.ts</c>.
/// </summary>
public static class FileValidationConstants
{
    /// <summary>
    /// The most image IDs that one link request may hold. A description holds at most 20,000 HTML characters
    /// (<c>RichTextConstants.HtmlMaxLength</c>), which is about 400 image tags of about 50 characters. A change of the
    /// HTML cap reviews this value.
    /// </summary>
    public const int LinkIdsMaxCount = 1000;

    public static class Text
    {
        public const int FileNameMaxLength = 255;
        public const int DescriptionMaxLength = 500;
    }

    public static class Size
    {
        /// <summary>The largest attachment: 100 MiB.</summary>
        public const long AttachmentMaxBytes = 104857600;

        /// <summary>The largest embedded image: 5 MiB.</summary>
        public const long EmbeddedMaxBytes = 5242880;

        /// <summary>The room that a multipart body needs above the file: 1 MiB.</summary>
        public const long MultipartOverheadBytes = 1048576;
    }
}
