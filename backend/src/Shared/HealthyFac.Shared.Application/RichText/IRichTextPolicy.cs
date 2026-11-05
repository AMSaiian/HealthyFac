namespace HealthyFac.Shared.Application.RichText;

/// <summary>
/// Decides whether a rich text string (HTML) holds only the formats that the editors of the web app emit. The policy
/// only judges. It never changes the text, so a validator rejects a request instead of altering the content (rule
/// <b>Text Input</b>).
/// </summary>
public interface IRichTextPolicy
{
    /// <summary>
    /// Checks the HTML against the allow-list of tags, attributes, classes, and URL schemes.
    /// </summary>
    /// <param name="html">The HTML to check.</param>
    /// <returns><see langword="true"/> when the HTML holds nothing outside the allow-list.</returns>
    bool IsAllowed(string html);
}
