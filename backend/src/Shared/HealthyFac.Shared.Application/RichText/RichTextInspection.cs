namespace HealthyFac.Shared.Application.RichText;

/// <summary>
/// The result of one parse of a rich text string.
/// </summary>
public sealed record RichTextInspection
{
    /// <summary>Whether the HTML holds only the allowed tags, attributes, classes, and URL schemes.</summary>
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// The number of visible UTF-16 characters: the text content of the parsed body after Unicode normalization form
    /// C (NFC). Tags, line breaks, and empty markup count 0, and an entity counts as its character. It is only
    /// meaningful when <see cref="IsAllowed"/> is <see langword="true"/>.
    /// </summary>
    public required int VisibleLength { get; init; }
}
