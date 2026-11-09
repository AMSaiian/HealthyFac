using FileSignatures;
using FileSignatures.Formats;

namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Lists the file types that the service accepts. The extension and the media type of a binary type come from its
/// FileSignatures formats, so no string is copied. The frontend mirrors this list in <c>files-limits.ts</c>.
/// </summary>
public static class AllowedFileTypes
{
    public static readonly IReadOnlyList<AllowedFileType> All =
    [
        Binary(isImage: true, new Png()),
        Binary(isImage: true, ["jpeg"], new Jpeg(), new JpegJfif(), new JpegExif()),
        Binary(isImage: true, new Gif()),
        Binary(isImage: true, new Webp()),
        Binary(isImage: false, new Pdf(), new AdobePdf()),
        Binary(isImage: false, new Word()),
        Binary(isImage: false, new Excel()),
        Binary(isImage: false, new PowerPoint()),
        Text(extension: "txt", mediaType: "text/plain"),
        Text(extension: "csv", mediaType: "text/csv"),
        Text(extension: "md", mediaType: "text/markdown"),
    ];

    /// <summary>
    /// Finds the type of a client extension, ignoring case.
    /// </summary>
    /// <param name="extension">The extension without the dot, or <see langword="null"/>.</param>
    /// <returns>The type, or <see langword="null"/> when the service accepts no such type.</returns>
    public static AllowedFileType? FindByExtension(string? extension) =>
        extension is null
            ? null
            : All.SingleOrDefault(type =>
                string.Equals(type.Extension, extension, StringComparison.OrdinalIgnoreCase)
                || type.AlternateExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Finds the type of a detected format. The macro-enabled and legacy Office formats derive from the accepted ones,
    /// so the lookup compares the exact type and never uses <c>is</c>.
    /// </summary>
    /// <param name="format">The format that FileSignatures detected.</param>
    /// <returns>The type, or <see langword="null"/> when the service refuses the format.</returns>
    public static AllowedFileType? FindByFormat(FileFormat format) =>
        All.SingleOrDefault(type => type.Formats.Contains(format.GetType()));

    /// <summary>
    /// Builds a binary type. It reads the extension and the media type of the first format.
    /// </summary>
    /// <param name="isImage">Whether the type is an image.</param>
    /// <param name="formats">The formats of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="InvalidOperationException">
    /// The formats of the type disagree on the extension or the media type.
    /// </exception>
    private static AllowedFileType Binary(bool isImage, params FileFormat[] formats) =>
        Binary(isImage, [], formats);

    /// <inheritdoc cref="Binary(bool, FileFormat[])"/>
    /// <param name="alternateExtensions">The other extensions that map to the type.</param>
    private static AllowedFileType Binary(bool isImage, string[] alternateExtensions, params FileFormat[] formats)
    {
        var first = formats[0];

        if (formats.Any(format => format.Extension != first.Extension || format.MediaType != first.MediaType))
        {
            throw new InvalidOperationException(
                $"The formats of the file type '{first.Extension}' disagree on the extension or the media type.");
        }

        return new AllowedFileType
        {
            Extension = first.Extension,
            MediaType = first.MediaType,
            AlternateExtensions = alternateExtensions,
            IsImage = isImage,
            Formats = formats.Select(format => format.GetType()).ToArray(),
        };
    }

    private static AllowedFileType Text(string extension, string mediaType) =>
        new() { Extension = extension, MediaType = mediaType };
}
