using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealthyFac.Seeder.Moments;

/// <summary>Reads and writes a <see cref="RelativeMoment"/> as its text form.</summary>
public sealed class RelativeMomentJsonConverter : JsonConverter<RelativeMoment>
{
    /// <exception cref="JsonException">The value is not a string that follows one of the moment forms.</exception>
    public override RelativeMoment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        try
        {
            return RelativeMoment.Parse(text ?? string.Empty);
        }
        catch (MomentException exception)
        {
            throw new JsonException(exception.Message, exception);
        }
    }

    public override void Write(Utf8JsonWriter writer, RelativeMoment value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
