using System.Text;
using System.Text.RegularExpressions;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Insights.Application.Extraction.Proposals;

/// <summary>
/// Cleans the text values that the model sends to an extractor tool. The model is untrusted, so each value becomes
/// one clean line (or plain paragraphs in HTML for a description) before the request validator checks its limits.
/// </summary>
public static partial class ProposalText
{
    private const char Space = ' ';

    /// <summary>
    /// Makes one line: control characters become spaces, the text is normalized, and runs of white space collapse.
    /// </summary>
    /// <param name="value">The text of the model.</param>
    /// <returns>The line, or <see langword="null"/> when nothing is left.</returns>
    public static string? SingleLine(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? Space : character);
        }

        var cleaned = WhitespacePattern().Replace(TextInputPolicy.Normalize(builder.ToString()), Space.ToString());

        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>
    /// Makes a source location: one line, cut to the limit of the message.
    /// </summary>
    /// <param name="value">The location label of the model.</param>
    /// <returns>The label, or <see langword="null"/> when nothing is left.</returns>
    public static string? Location(string? value)
    {
        var cleaned = SingleLine(value);

        return cleaned is null || cleaned.Length <= DraftMessageLimits.SourceLocationMaxLength
            ? cleaned
            : cleaned[..DraftMessageLimits.SourceLocationMaxLength];
    }

    /// <summary>
    /// Makes the HTML of a description from the plain text of the model.
    /// </summary>
    /// <param name="value">The plain text.</param>
    /// <returns>The HTML, or <see langword="null"/> when the text holds no paragraph.</returns>
    public static string? Description(string? value) =>
        PlainTextHtml.FromPlainText(value is null ? null : TextInputPolicy.Normalize(value));

    /// <summary>
    /// Cleans each state of a state set and keeps one of each.
    /// </summary>
    /// <param name="states">The states of the model.</param>
    /// <returns>The distinct states, or <see langword="null"/> when the model sent none.</returns>
    public static IReadOnlyList<string>? States(IReadOnlyList<string>? states) =>
        states is null
            ? null
            : [.. states.Select(SingleLine).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Keeps a number only when it is finite.
    /// </summary>
    /// <param name="value">The number of the model.</param>
    /// <returns>The number, or <see langword="null"/> for NaN and infinity.</returns>
    public static double? Finite(double? value) => value is { } number && double.IsFinite(number) ? number : null;

    [GeneratedRegex(pattern: @"\s+")]
    private static partial Regex WhitespacePattern();
}
