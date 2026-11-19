namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// Where and why an attempt failed, for a failure that a person must understand (for example <c>unsafeContent</c>).
/// The detail holds a place in the file and a short reason, never the text of the file.
/// </summary>
public readonly record struct IngestErrorDetail
{
    /// <summary>The place in the file, for example <c>page 3</c>.</summary>
    public required string Location { get; init; }

    /// <summary>The short reason that the screener gave.</summary>
    public required string Reason { get; init; }
}
