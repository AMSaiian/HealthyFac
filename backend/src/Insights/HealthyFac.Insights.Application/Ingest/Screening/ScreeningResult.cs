namespace HealthyFac.Insights.Application.Ingest.Screening;

/// <summary>
/// The result of the screening of the chunks of a file. The screening stops at the first unsafe chunk, so the result
/// names at most one.
/// </summary>
public record ScreeningResult
{
    /// <summary>The place in the file of the first unsafe chunk, or <see langword="null"/> when all are safe.</summary>
    public string? UnsafeLocation { get; init; }

    /// <summary>The short reason of the screener for the first unsafe chunk, or <see langword="null"/>.</summary>
    public string? UnsafeReason { get; init; }

    /// <summary>What the screening did, for the <c>screener</c> run.</summary>
    public required ScreeningRunLog Run { get; init; }

    public bool IsSafe => UnsafeLocation is null;
}
