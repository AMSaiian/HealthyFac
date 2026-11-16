using System.Globalization;
using System.Text.Json;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Writes the JSON payload of one machine message from a scenario sample. The payload keeps only the keys and the
/// codes that the definitions of the machine hold, so a missing definition never makes the intake reject a message.
/// </summary>
public static class MachinePayloadBuilder
{
    /// <summary>
    /// Builds the payload.
    /// </summary>
    /// <param name="sample">The sample of the scenario.</param>
    /// <param name="definitions">The definitions of the machine.</param>
    /// <param name="sampledAt">The sampling time. The payload carries it as a UTC time with <c>Z</c>.</param>
    /// <returns>The JSON bytes and the keys that the builder skipped for a wrong value kind.</returns>
    public static BuiltPayload Build(ScenarioSample sample, MachineDefinitions definitions, DateTime sampledAt)
    {
        var skipped = new List<string>();
        var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString(
                MetricSchemaConstants.Fields.SampledAt,
                sampledAt.ToUniversalTime().ToString(format: "O", CultureInfo.InvariantCulture));

            writer.WriteStartObject(MetricSchemaConstants.Fields.Metrics);
            foreach (var metric in definitions.Metrics)
            {
                if (sample.Values.TryGetValue(metric.Key, out var value) && !TryWrite(writer, metric, value))
                {
                    skipped.Add(metric.Key);
                }
            }

            writer.WriteEndObject();

            writer.WriteStartArray(MetricSchemaConstants.Fields.Statuses);
            foreach (var code in sample.StatusCodes.Where(definitions.StatusCodes.Contains).Distinct())
            {
                writer.WriteStringValue(code);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return new BuiltPayload { Json = stream.ToArray(), SkippedKeys = skipped };
    }

    private static bool TryWrite(Utf8JsonWriter writer, MetricInfo metric, object value)
    {
        if (metric.ValueKind == MetricValueKind.Numeric && value is double number && double.IsFinite(number))
        {
            writer.WriteNumber(metric.Key, number);
        }
        else if (metric.ValueKind == MetricValueKind.Boolean && value is bool flag)
        {
            writer.WriteBoolean(metric.Key, flag);
        }
        else if (metric.ValueKind == MetricValueKind.StateSet && value is string state)
        {
            writer.WriteString(metric.Key, state);
        }
        else
        {
            return false;
        }

        return true;
    }
}
