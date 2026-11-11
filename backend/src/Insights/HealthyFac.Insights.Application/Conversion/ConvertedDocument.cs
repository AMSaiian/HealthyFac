namespace HealthyFac.Insights.Application.Conversion;

/// <summary>
/// The result of the conversion of a file: the number of pages and the text chunks in reading order.
/// </summary>
public record ConvertedDocument
{
    public required int PageCount { get; init; }

    public required IReadOnlyList<ConvertedChunk> Chunks { get; init; }
}

/// <summary>
/// One text chunk of a converted file.
/// </summary>
public record ConvertedChunk
{
    /// <summary>The page, sheet, or slide, and the heading path, or <see langword="null"/> when none.</summary>
    public string? Location { get; init; }

    public required string Text { get; init; }
}
