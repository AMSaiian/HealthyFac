namespace HealthyFac.Insights.Application.Files;

/// <summary>
/// A file with a signed link for the download. The worker reads the content through the link.
/// </summary>
public record IngestFileLink
{
    public required IngestFile File { get; init; }

    public required string Url { get; init; }

    public required DateTime ExpiresAt { get; init; }
}
