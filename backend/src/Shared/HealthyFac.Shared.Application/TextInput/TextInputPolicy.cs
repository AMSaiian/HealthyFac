using System.Reflection;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Defines the one normalization and the one character check of text input, so the JSON converters and the
/// validator cannot differ. Normalization changes only what has no meaning (outer whitespace, the Unicode form, line
/// endings). It never removes a character: the check rejects it instead.
/// </summary>
public static class TextInputPolicy
{
    private const char LineFeed = '\n';
    private const char Tab = '\t';
    private const string CarriageReturnLineFeed = "\r\n";
    private const string CarriageReturn = "\r";
    private const string LineFeedText = "\n";

    /// <summary>
    /// Reads the form of a property from its <see cref="MultilineTextAttribute"/> or <see cref="RawTextAttribute"/>.
    /// </summary>
    /// <param name="property">The property, or <see langword="null"/> when the value has no property.</param>
    /// <returns>The form; <see cref="TextInputKind.SingleLine"/> without an attribute.</returns>
    public static TextInputKind KindOf(ICustomAttributeProvider? property)
    {
        if (property?.IsDefined(typeof(RawTextAttribute), true) == true)
        {
            return TextInputKind.Raw;
        }

        return property?.IsDefined(typeof(MultilineTextAttribute), true) == true
            ? TextInputKind.Multiline
            : TextInputKind.SingleLine;
    }

    /// <summary>
    /// Trims both ends and applies Unicode NFC. Multi-line text also gets <c>\n</c> line endings. Raw text stays
    /// unchanged.
    /// </summary>
    /// <param name="value">The text as received.</param>
    /// <param name="kind">The form of the text.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid Unicode (e.g., a lone
    /// surrogate), so NFC cannot apply.</exception>
    public static string Normalize(string value, TextInputKind kind) =>
        kind switch
        {
            TextInputKind.Raw => value,
            TextInputKind.Multiline => value
                .Replace(CarriageReturnLineFeed, LineFeedText, StringComparison.Ordinal)
                .Replace(CarriageReturn, LineFeedText, StringComparison.Ordinal)
                .Trim()
                .Normalize(),
            _ => value.Trim().Normalize(),
        };

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
        if (value.Contains('\0', StringComparison.Ordinal))
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
