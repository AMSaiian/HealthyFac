namespace HealthyFac.Document.Application.Files.Links;

/// <summary>
/// A signed, short-lived URL that reads one file.
/// </summary>
public record FileLink
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required DateTime ExpiresAt { get; init; }
}
