using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.TextInput;

/// <summary>
/// Adds the text input normalization to JSON options. The API options and the messaging options both call it, so a
/// request and a message read the same text in the same way.
/// </summary>
public static class TextInputJson
{
    private static readonly TextInputJsonConverter Converter = new();

    /// <summary>
    /// Adds a converter for every string, which also covers the strings in collections and nested objects.
    /// </summary>
    /// <param name="options">The options to change. They must not be read-only yet.</param>
    public static void Configure(JsonSerializerOptions options) => options.Converters.Add(Converter);
}
