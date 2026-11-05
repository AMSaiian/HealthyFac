namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Defines the one normalization and the one character check of text input, so the JSON converter and the
/// validation rules cannot differ. Normalization changes only what has no meaning (outer whitespace, the Unicode
/// form, line endings). It never removes a character: the check rejects it instead.
/// </summary>
public static class TextInputPolicy
{
    private const char LineFeed = '\n';
    private const char Tab = '\t';
    private const string CarriageReturnLineFeed = "\r\n";
    private const string CarriageReturn = "\r";
    private const string LineFeedText = "\n";

    /// <summary>
    /// Turns <c>\r\n</c> and <c>\r</c> into <c>\n</c>, trims both ends, and applies Unicode NFC.
    /// </summary>
    /// <param name="value">The text as received.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid Unicode (e.g., a lone
    /// surrogate), so NFC cannot apply.</exception>
    public static string Normalize(string value) =>
        value
            .Replace(CarriageReturnLineFeed, LineFeedText, StringComparison.Ordinal)
            .Replace(CarriageReturn, LineFeedText, StringComparison.Ordinal)
            .Trim()
            .Normalize();

    /// <summary>
    /// Finds the first rule that the text breaks: the null character in any form, any control character in
    /// single-line text, and any control character except <c>\n</c> and <c>\t</c> in multi-line text.
    /// </summary>
    /// <param name="value">The normalized text.</param>
    /// <param name="kind">The form of the text.</param>
    /// <returns>The error message from <see cref="TextInputErrors"/>, or <see langword="null"/> when the text is
    /// valid.</returns>
    public static string? FindViolation(string value, TextInputKind kind)
    {
        if (value.Contains(value: '\0', StringComparison.Ordinal))
        {
            return TextInputErrors.NullCharacter;
        }

        return kind switch
        {
            TextInputKind.SingleLine when value.Any(char.IsControl) => TextInputErrors.ControlCharacter,
            TextInputKind.Multiline when value.Any(IsForbiddenInMultiline) =>
                TextInputErrors.MultilineControlCharacter,
            _ => null,
        };
    }

    private static bool IsForbiddenInMultiline(char character) =>
        char.IsControl(character) && character != LineFeed && character != Tab;
}
