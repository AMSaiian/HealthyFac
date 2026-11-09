using FileSignatures.Formats;
using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Infrastructure.Files.Inspection;

/// <summary>
/// Maps the exact format types of FileSignatures to the file types that the service accepts. The macro-enabled and
/// legacy Office formats derive from the accepted ones, so the lookup compares the exact type and never uses
/// <c>is</c>. A type that is not in the map is refused.
/// </summary>
public static class FileFormatMap
{
    private static readonly IReadOnlyDictionary<Type, string> Extensions = new Dictionary<Type, string>
    {
        [typeof(Png)] = "png",
        [typeof(Jpeg)] = "jpg",
        [typeof(JpegJfif)] = "jpg",
        [typeof(JpegExif)] = "jpg",
        [typeof(Gif)] = "gif",
        [typeof(Webp)] = "webp",
        [typeof(Pdf)] = "pdf",
        [typeof(AdobePdf)] = "pdf",
        [typeof(Word)] = "docx",
        [typeof(Excel)] = "xlsx",
        [typeof(PowerPoint)] = "pptx",
    };

    /// <summary>The format types that the map accepts.</summary>
    public static IEnumerable<Type> FormatTypes => Extensions.Keys;

    /// <summary>
    /// Finds the accepted type of a detected format.
    /// </summary>
    /// <param name="format">The format that FileSignatures detected.</param>
    /// <returns>The type, or <see langword="null"/> when the service refuses the format.</returns>
    public static AllowedFileType? Find(FileSignatures.FileFormat format) =>
        Extensions.TryGetValue(format.GetType(), out var extension)
            ? AllowedFileTypes.FindByExtension(extension)
            : null;
}
