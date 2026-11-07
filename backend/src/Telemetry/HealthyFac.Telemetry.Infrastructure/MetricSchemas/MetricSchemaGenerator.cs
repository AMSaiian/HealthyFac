using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Json.Schema;

namespace HealthyFac.Telemetry.Infrastructure.MetricSchemas;

/// <summary>
/// Builds the JSON Schema (draft 2020-12) of the payload of one machine. This is the only definition of the payload
/// rules: the intake evaluates the stored schema and has no second rule set.
/// </summary>
/// <remarks>
/// The schema asserts the envelope and the JSON type of each key. The range, the states, the unit, and the title are
/// annotations, so a value outside the range or the states stays valid and the intake only flags it. Every build uses
/// its own schema registry, because the global registry throws when two schemas share an ID, and the same machine and
/// revision can be built twice (a parallel apply).
/// </remarks>
internal sealed class MetricSchemaGenerator : IMetricSchemaGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Generate(string gatewayId, int revision, IEnumerable<MetricSchema> metrics)
    {
        var metricList = metrics.OrderBy(metric => metric.Key, StringComparer.Ordinal).ToList();

        var metricProperties = metricList
            .Select(metric => (metric.Key, BuildMetric(metric)))
            .ToArray();

        var schema = new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft202012Id)
            .Id(string.Format(CultureInfo.InvariantCulture, MetricSchemaConstants.IdFormat, gatewayId, revision))
            .Type(SchemaValueType.Object)
            .Properties(
                (MetricSchemaConstants.Fields.SampledAt,
                    new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.DateTime)),
                (MetricSchemaConstants.Fields.Metrics,
                    new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(metricProperties)
                        .Required(metricList.Select(metric => metric.Key))
                        .AdditionalProperties(JsonSchemaBuilder.False)),
                (MetricSchemaConstants.Fields.Statuses,
                    new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                        .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))))
            .Required(
                MetricSchemaConstants.Fields.SampledAt,
                MetricSchemaConstants.Fields.Metrics,
                MetricSchemaConstants.Fields.Statuses)
            .AdditionalProperties(JsonSchemaBuilder.False)
            .Build(new BuildOptions { SchemaRegistry = new SchemaRegistry() });

        return JsonSerializer.Serialize(schema, SerializerOptions);
    }

    private static JsonSchemaBuilder BuildMetric(MetricSchema metric)
    {
        var builder = new JsonSchemaBuilder()
            .Type(ToJsonType(metric.ValueKind))
            .Title(metric.Name);

        if (metric.UnitQuantity is not null && metric.UnitName is not null)
        {
            builder.Unrecognized(
                MetricSchemaConstants.Annotations.Unit,
                new JsonObject
                {
                    [MetricSchemaConstants.Annotations.UnitQuantity] = metric.UnitQuantity,
                    [MetricSchemaConstants.Annotations.UnitName] = metric.UnitName,
                });
        }

        if (metric.Min is not null || metric.Max is not null)
        {
            var range = new JsonObject();

            if (metric.Min is { } min)
            {
                range[MetricSchemaConstants.Annotations.RangeMin] = min;
            }

            if (metric.Max is { } max)
            {
                range[MetricSchemaConstants.Annotations.RangeMax] = max;
            }

            builder.Unrecognized(MetricSchemaConstants.Annotations.Range, range);
        }

        if (metric.States is not null)
        {
            builder.Unrecognized(
                MetricSchemaConstants.Annotations.States,
                new JsonArray(metric.States.Select(state => (JsonNode)state).ToArray()));
        }

        return builder;
    }

    private static SchemaValueType ToJsonType(MetricValueKind kind)
    {
        if (kind == MetricValueKind.Numeric)
        {
            return SchemaValueType.Number;
        }

        return kind == MetricValueKind.Boolean ? SchemaValueType.Boolean : SchemaValueType.String;
    }
}
