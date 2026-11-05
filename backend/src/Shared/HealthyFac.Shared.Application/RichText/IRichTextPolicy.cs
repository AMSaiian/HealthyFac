namespace HealthyFac.Shared.Application.RichText;

/// <summary>
/// Decides whether a rich text string (HTML) holds only the formats that the editors of the web app emit, and counts
/// its visible text. The policy only judges. It never changes the text, so a validator rejects a request instead of
/// altering the content (rule <b>Text Input</b>).
/// </summary>
public interface IRichTextPolicy
{
    /// <summary>
    /// Parses the HTML once, checks it against the allow-list of tags, attributes, classes, and URL schemes, and
    /// counts the visible text.
    /// </summary>
    /// <param name="html">The HTML to inspect.</param>
    /// <returns>The allow-list verdict and the visible length.</returns>
    RichTextInspection Inspect(string html);
}
