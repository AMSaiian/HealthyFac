using System.Text;
using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

internal static class JsonTopLevelFields
{
    /// <summary>
    /// Reads the scalar properties of the root object of a JSON text: strings, numbers, and booleans. The text may
    /// end inside a value, so a long error body can be read from its first part.
    /// </summary>
    /// <remarks>
    /// The method collects only the properties of the root object. It skips every nested object and array, so a
    /// field with the same name inside a nested value is never collected. The first occurrence of a name wins. A
    /// <c>null</c> value is left out. When the text ends inside a value (<paramref name="isComplete"/> is
    /// <see langword="false"/>), the method stops at that value without an exception and returns the fields before
    /// it. A string or a number that the end of the text breaks is lost. Invalid JSON throws
    /// <see cref="JsonException"/>.
    /// </remarks>
    /// <param name="json">The UTF-8 bytes of the JSON text, or of its first part.</param>
    /// <param name="isComplete"><see langword="true"/> when <paramref name="json"/> is the whole text.</param>
    /// <returns>The scalar properties by exact name. The result is empty when the root is not an object.</returns>
    public static IReadOnlyDictionary<string, JsonTopLevelField> Read(ReadOnlySpan<byte> json, bool isComplete)
    {
        var fields = new Dictionary<string, JsonTopLevelField>(StringComparer.Ordinal);
        var reader = new Utf8JsonReader(json, isComplete, default(JsonReaderState));
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return fields;
        }

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var name = reader.GetString();
            if (!reader.Read())
            {
                return fields;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                if (!reader.TrySkip())
                {
                    return fields;
                }
            }
            else if (name is not null && ToField(ref reader) is { } field)
            {
                fields.TryAdd(name, field);
            }
        }

        return fields;
    }

    private static JsonTopLevelField? ToField(ref Utf8JsonReader reader) =>
        reader.TokenType switch
        {
            JsonTokenType.String when reader.GetString() is { } text => new JsonTopLevelField
            {
                Kind = JsonValueKind.String,
                Text = text,
            },
            JsonTokenType.Number => new JsonTopLevelField
            {
                Kind = JsonValueKind.Number,
                Text = Encoding.UTF8.GetString(reader.ValueSpan),
            },
            JsonTokenType.True => new JsonTopLevelField { Kind = JsonValueKind.True, Text = "true" },
            JsonTokenType.False => new JsonTopLevelField { Kind = JsonValueKind.False, Text = "false" },
            _ => null,
        };
}
