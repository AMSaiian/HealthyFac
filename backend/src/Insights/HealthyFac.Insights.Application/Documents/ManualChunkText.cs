namespace HealthyFac.Insights.Application.Documents;

/// <summary>
/// Reads the shape of a chunk text. A chunk that only repeats a title adds nothing for the model.
/// </summary>
public static class ManualChunkText
{
    private const string HeadingMarker = "#";
    private const string BoldMarker = "**";

    /// <summary>
    /// Tells whether the text has only Markdown heading lines (<c>#</c>) and fully bold lines (<c>**...**</c>), or no
    /// line at all.
    /// </summary>
    /// <param name="text">The text of the chunk without the data block tags.</param>
    /// <returns><see langword="true"/> for a title-only chunk.</returns>
    public static bool IsTitleOnly(string text) =>
        text.Split(separator: '\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(line => line.StartsWith(HeadingMarker, StringComparison.Ordinal)
                || (line.Length > BoldMarker.Length * 2
                    && line.StartsWith(BoldMarker, StringComparison.Ordinal)
                    && line.EndsWith(BoldMarker, StringComparison.Ordinal)));
}
