using System.Text.Json;
using HealthyFac.Telemetry.Application.MetricSchemas;
using Json.Pointer;
using Json.Schema;
using Json.Schema.Keywords;

namespace HealthyFac.Telemetry.Infrastructure.Intake;

/// <summary>
/// Holds what one evaluation of the payload against the stored schema found, sorted by the place in the payload:
/// the fields and metrics that the schema does not know, and the places whose JSON type or format is wrong.
/// </summary>
/// <remarks>
/// The evaluation runs once in the list output format. Each failed result names the place (<c>InstanceLocation</c>)
/// and the keyword, so the findings need no second evaluation of a part of the payload. The <c>required</c> result
/// names the missing properties only as text, so the parser compares the property names itself.
/// </remarks>
internal sealed class SchemaFindings
{
    private SchemaFindings()
    {
    }

    public bool IsPayloadTypeInvalid { get; private set; }

    public bool IsMetricsTypeInvalid { get; private set; }

    public bool IsStatusesInvalid { get; private set; }

    public bool IsSampledAtInvalid { get; private set; }

    public HashSet<string> UnknownFields { get; } = [];

    public HashSet<string> UnknownMetrics { get; } = [];

    public HashSet<string> InvalidMetricValues { get; } = [];

    public static SchemaFindings Evaluate(JsonSchema schema, JsonElement payload)
    {
        var results = schema.Evaluate(
            payload,
            new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true });
        var findings = new SchemaFindings();

        foreach (var detail in (results.Details ?? []).Where(detail => !detail.IsValid))
        {
            findings.Add(detail);
        }

        return findings;
    }

    private static string[] Segments(JsonPointer pointer) =>
        Enumerable.Range(start: 0, pointer.SegmentCount).Select(index => pointer[index].ToString()).ToArray();

    private void Add(EvaluationResults detail)
    {
        var location = Segments(detail.InstanceLocation);
        var isUnknownKey = Segments(detail.EvaluationPath).LastOrDefault() == AdditionalPropertiesKeyword.Instance.Name;
        var isTypeFailure = detail.Errors?.ContainsKey(TypeKeyword.Instance.Name) == true;
        var isFormatFailure = detail.Errors?.ContainsKey(FormatKeyword.Instance.Name) == true;

        switch (location)
        {
            case [] when isTypeFailure:
                IsPayloadTypeInvalid = true;

                break;
            case [var field] when isUnknownKey:
                UnknownFields.Add(field);

                break;
            case [MetricSchemaConstants.Fields.SampledAt] when isTypeFailure || isFormatFailure:
                IsSampledAtInvalid = true;

                break;
            case [MetricSchemaConstants.Fields.Metrics] when isTypeFailure:
                IsMetricsTypeInvalid = true;

                break;
            case [MetricSchemaConstants.Fields.Statuses, ..] when isTypeFailure:
                IsStatusesInvalid = true;

                break;
            case [MetricSchemaConstants.Fields.Metrics, var key] when isUnknownKey:
                UnknownMetrics.Add(key);

                break;
            case [MetricSchemaConstants.Fields.Metrics, var key] when isTypeFailure:
                InvalidMetricValues.Add(key);

                break;
        }
    }
}
