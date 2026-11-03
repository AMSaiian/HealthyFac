namespace HealthyFac.Spa.Web.IndexDocument;

/// <summary>
/// Defines the exceptions of the index document.
/// </summary>
public static class IndexDocumentErrors
{
    public static FileNotFoundException IndexFileMissing(string path) =>
        new($"The index file {path} does not exist. Build the app and set Spa:ContentRoot to its output folder.", path);
}
