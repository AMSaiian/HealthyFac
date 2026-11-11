using System.Collections.Frozen;

namespace HealthyFac.Insights.Application.Ingest;

/// <summary>
/// Defines the rules that decide which files and how many attempts the ingest accepts.
/// </summary>
public static class IngestRules
{
    /// <summary>The number of failed attempts after which a file gets no new attempt.</summary>
    public const int MaxFailedAttempts = 3;

    /// <summary>The purpose of a file that a user can ingest. An embedded image is not ingested.</summary>
    public const string AttachmentPurpose = "attachment";

    /// <summary>The most attempts that the list of a file returns.</summary>
    public const int MaxListedAttempts = 50;

    /// <summary>The lower case extensions that the ingest reads.</summary>
    public static readonly FrozenSet<string> IngestableExtensions =
        new[] { "pdf", "docx", "xlsx", "pptx", "txt", "csv", "md" }.ToFrozenSet();
}
