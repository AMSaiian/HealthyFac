namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Lists the file types that the service accepts. The frontend mirrors this list in <c>files-limits.ts</c>.
/// </summary>
public static class AllowedFileTypes
{
    public static readonly IReadOnlyList<AllowedFileType> All =
    [
        Image(extension: "png", mediaType: "image/png"),
        Image(extension: "jpg", mediaType: "image/jpeg", "jpeg"),
        Image(extension: "gif", mediaType: "image/gif"),
        Image(extension: "webp", mediaType: "image/webp"),
        Document(extension: "pdf", mediaType: "application/pdf"),
        Document(
            extension: "docx", mediaType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        Document(extension: "xlsx", mediaType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        Document(
            extension: "pptx", mediaType: "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
        Document(extension: "txt", mediaType: "text/plain"),
        Document(extension: "csv", mediaType: "text/csv"),
        Document(extension: "md", mediaType: "text/markdown"),
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

    private static AllowedFileType Image(string extension, string mediaType, params string[] alternateExtensions) =>
        new()
        {
            Extension = extension,
            MediaType = mediaType,
            AlternateExtensions = alternateExtensions,
            IsImage = true,
        };

    private static AllowedFileType Document(string extension, string mediaType) =>
        new() { Extension = extension, MediaType = mediaType };
}
