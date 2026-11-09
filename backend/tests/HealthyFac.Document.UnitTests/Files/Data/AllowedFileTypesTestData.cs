namespace HealthyFac.Document.UnitTests.Files.Data;

public static class AllowedFileTypesTestData
{
    /// <summary>
    /// The stored extension and media type of each allowed type, in list order. The database pins the extensions.
    /// </summary>
    public static readonly IReadOnlyList<(string Extension, string MediaType)> StoredTypes =
    [
        ("png", "image/png"),
        ("jpg", "image/jpeg"),
        ("gif", "image/gif"),
        ("webp", "image/webp"),
        ("pdf", "application/pdf"),
        ("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        ("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        ("pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
        ("txt", "text/plain"),
        ("csv", "text/csv"),
        ("md", "text/markdown"),
    ];
}
