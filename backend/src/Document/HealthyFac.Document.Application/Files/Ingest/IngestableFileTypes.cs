namespace HealthyFac.Document.Application.Files.Ingest;

/// <summary>
/// Defines which files a user can send to the ingest, and how often a failed ingest may repeat.
/// </summary>
public static class IngestableFileTypes
{
    /// <summary>
    /// The number of failed or interrupted attempts after which the grid offers no new ingest.
    /// </summary>
    /// <remarks>
    /// Insights enforces the same limit when it starts an attempt (<c>IngestRules.MaxFailedAttempts</c>).
    /// </remarks>
    public const int MaxFailedAttempts = 3;

    /// <summary>The lower case extensions that Insights can convert.</summary>
    public static readonly string[] Extensions = ["pdf", "docx", "xlsx", "pptx", "txt", "csv", "md"];
}
