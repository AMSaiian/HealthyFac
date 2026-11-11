using System.Net;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Turns plain text of the model into the simple HTML that the descriptions use.
/// </summary>
public static class PlainTextHtml
{
    private static readonly string[] ParagraphSeparators = ["\r\n\r\n", "\n\n", "\r\r"];

    /// <summary>
    /// Splits the text on blank lines and wraps each paragraph in <c>&lt;p&gt;</c> with HTML encoding, so text such as
    /// <c>&lt;script&gt;</c> becomes harmless text.
    /// </summary>
    /// <param name="text">The plain text.</param>
    /// <returns>The HTML, or <see langword="null"/> when the text holds no paragraph.</returns>
    public static string? FromPlainText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var paragraphs = text
            .Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => $"<p>{WebUtility.HtmlEncode(paragraph)}</p>")
            .ToList();

        return paragraphs.Count == 0 ? null : string.Concat(paragraphs);
    }
}
