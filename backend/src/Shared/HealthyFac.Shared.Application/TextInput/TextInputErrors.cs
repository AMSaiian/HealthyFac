using System.Text.Json;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Defines the rejections of text input. A message names the rule, never the value: the property name of the
/// failure names the field.
/// </summary>
public static class TextInputErrors
{
    public const string NullCharacter = "The text must not contain the null character.";
    public const string ControlCharacter = "The text must not contain control characters.";

    public const string MultilineControlCharacter =
        "The text must not contain control characters other than line breaks and tabs.";

    public const string InvalidUnicode = "The text must hold valid Unicode.";

    /// <summary>
    /// Creates the error for a string that Unicode normalization cannot read (e.g., a lone surrogate). The
    /// serializer reports it as invalid JSON, so HTTP answers <c>400</c> and a message fails at the first attempt.
    /// </summary>
    /// <param name="innerException">The failure of the normalization.</param>
    /// <returns>The exception to throw from a JSON converter.</returns>
    public static JsonException InvalidUnicodeText(Exception innerException) => new(InvalidUnicode, innerException);
}
