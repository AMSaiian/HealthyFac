using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealthyFac.Insights.Infrastructure.Agents.Tools;

/// <summary>
/// Reads a time argument of a tool as UTC. <see cref="Utf8JsonReader.TryGetDateTime"/> turns a time with an offset
/// into the local time of the server, so the converter converts it back to UTC. A time without an offset means UTC.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.TryGetDateTime(out var value))
        {
            throw new JsonException();
        }

        return value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value,
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
