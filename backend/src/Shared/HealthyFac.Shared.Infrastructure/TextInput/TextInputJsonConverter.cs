using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.Infrastructure.TextInput;

/// <summary>
/// Normalizes a string on read with <see cref="TextInputPolicy.Normalize"/> for one form. It writes the string
/// unchanged, so a response or an outgoing message shows the stored value. A <see langword="null"/> token stays
/// <see langword="null"/>, because the serializer handles it before the converter.
/// </summary>
/// <param name="kind">The form that the converter applies.</param>
internal sealed class TextInputJsonConverter(TextInputKind kind) : JsonConverter<string>
{
    private readonly TextInputKind _kind = kind;

    /// <summary>
    /// Reads the string and normalizes it for the form of the converter.
    /// </summary>
    /// <param name="reader">The reader at a string token.</param>
    /// <param name="typeToConvert">The type to convert, always <see cref="string"/>.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The normalized string.</returns>
    /// <exception cref="JsonException">Thrown when the string is not valid Unicode, so the serializer reports
    /// invalid JSON.</exception>
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()!;
        try
        {
            return TextInputPolicy.Normalize(value, _kind);
        }
        catch (ArgumentException exception)
        {
            throw TextInputErrors.InvalidUnicodeText(exception);
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);

    /// <summary>
    /// Reads a dictionary key unchanged. A key names a field; it is no text input.
    /// </summary>
    /// <param name="reader">The reader at a property name token.</param>
    /// <param name="typeToConvert">The type to convert, always <see cref="string"/>.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The key as received.</returns>
    public override string ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        reader.GetString()!;

    public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value);
}
