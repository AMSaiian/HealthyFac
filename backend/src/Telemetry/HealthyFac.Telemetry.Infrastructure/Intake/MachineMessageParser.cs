using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.Intake;
using Json.Schema;
using Json.Schema.Keywords;

namespace HealthyFac.Telemetry.Infrastructure.Intake;

/// <summary>
/// Checks the payload of a machine message against the stored schema of its machine. One evaluation of the whole
/// payload (<see cref="SchemaFindings"/>) gives the structure errors; the range or the states from the annotations
/// only flag a value. The schema is the only definition of the rules.
/// </summary>
/// <remarks>
/// The class parses the schema for each message with its own schema registry, because the global registry throws for
/// a second schema with the same ID, and it keeps no parsed schema between messages (no cache in process memory).
/// </remarks>
internal sealed class MachineMessageParser : IMachineMessageParser
{
    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly JsonDocumentOptions StrictJson = new() { AllowDuplicateProperties = false };

    public PayloadReading ReadPayload(byte[] payload)
    {
        string text;
        bool isUnstorable;

        try
        {
            text = StrictUtf8.GetString(payload);
            using var document = JsonDocument.Parse(text, StrictJson);
            isUnstorable = HasUnstorableText(document.RootElement);
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            return new PayloadReading { ErrorCode = IntakeErrorCodes.MalformedJson };
        }
        catch (InvalidOperationException)
        {
            return new PayloadReading { ErrorCode = IntakeErrorCodes.UnstorableJson };
        }

        return isUnstorable
            ? new PayloadReading { ErrorCode = IntakeErrorCodes.UnstorableJson }
            : new PayloadReading { Text = text };
    }

    public ParsedMachineMessage Parse(string schemaJson, string payloadText, DateTime receivedAt)
    {
        using var document = JsonDocument.Parse(payloadText, StrictJson);

        return Parse(schemaJson, document.RootElement, receivedAt);
    }

    /// <summary>
    /// Tells whether a string value or a property name holds text that <c>jsonb</c> cannot store: a U+0000 character,
    /// or a lone surrogate (an invalid UTF-16 escape, which <see cref="JsonElement.GetString"/> rejects).
    /// </summary>
    private static bool HasUnstorableText(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return IsUnstorable(element.GetString);
            case JsonValueKind.Array:
                return element.EnumerateArray().Any(HasUnstorableText);
            case JsonValueKind.Object:
                return element.EnumerateObject()
                    .Any(property => IsUnstorable(() => property.Name) || HasUnstorableText(property.Value));
            default:
                return false;
        }
    }

    private static bool IsUnstorable(Func<string?> read)
    {
        try
        {
            return read()?.Contains(value: '\0') ?? false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static ParsedMachineMessage Parse(string schemaJson, JsonElement payload, DateTime receivedAt)
    {
        var schema = JsonSchema.FromText(schemaJson, new BuildOptions { SchemaRegistry = new SchemaRegistry() });
        var findings = SchemaFindings.Evaluate(schema, payload);

        if (findings.IsPayloadTypeInvalid)
        {
            return EnvelopeFailure([Error(string.Empty, IntakeErrorCodes.WrongType)]);
        }

        var schemaNode = JsonNode.Parse(schemaJson)!;
        var envelopeErrors = CheckEnvelope(schemaNode, payload, findings);
        var sampledAt = ReadSampledAt(payload, findings, envelopeErrors);

        if (sampledAt is { } sampled && sampled > receivedAt + IntakeConstants.FutureTolerance)
        {
            envelopeErrors.Add(Error(MetricSchemaConstants.Fields.SampledAt, IntakeErrorCodes.FutureSampledAt));
        }

        if (envelopeErrors.Count > 0)
        {
            return new ParsedMachineMessage
            {
                Errors = envelopeErrors,
                SampledAt = sampledAt,
                IsEnvelopeFailure = true,
            };
        }

        var errors = new List<IntakeError>();
        var metrics = CheckMetrics(
            schemaNode,
            payload.GetProperty(MetricSchemaConstants.Fields.Metrics),
            findings,
            errors);
        var statusCodes = CheckStatuses(payload.GetProperty(MetricSchemaConstants.Fields.Statuses), errors);

        return new ParsedMachineMessage
        {
            Errors = errors,
            SampledAt = sampledAt,
            IsEnvelopeFailure = false,
            Metrics = metrics,
            StatusCodes = statusCodes,
        };
    }

    private static List<IntakeError> CheckEnvelope(JsonNode schemaNode, JsonElement payload, SchemaFindings findings)
    {
        var errors = new List<IntakeError>();
        var unknownFields = payload.EnumerateObject()
            .Where(property => findings.UnknownFields.Contains(property.Name));

        errors.AddRange(unknownFields.Select(property => Error(property.Name, IntakeErrorCodes.UnknownField)));

        foreach (var required in RequiredNames(schemaNode))
        {
            if (!payload.TryGetProperty(required, out _))
            {
                errors.Add(Error(required, IntakeErrorCodes.MissingField));
            }
            else if ((required == MetricSchemaConstants.Fields.Metrics && findings.IsMetricsTypeInvalid)
                || (required == MetricSchemaConstants.Fields.Statuses && findings.IsStatusesInvalid))
            {
                errors.Add(Error(required, IntakeErrorCodes.WrongType));
            }
        }

        return errors;
    }

    private static DateTime? ReadSampledAt(JsonElement payload, SchemaFindings findings, List<IntakeError> errors)
    {
        if (!payload.TryGetProperty(MetricSchemaConstants.Fields.SampledAt, out var value))
        {
            return null;
        }

        if (findings.IsSampledAtInvalid
            || !DateTimeOffset.TryParse(
                value.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var sampledAt))
        {
            errors.Add(Error(MetricSchemaConstants.Fields.SampledAt, IntakeErrorCodes.InvalidSampledAt));

            return null;
        }

        return sampledAt.UtcDateTime;
    }

    private static List<ParsedMetricValue> CheckMetrics(
        JsonNode schemaNode,
        JsonElement metrics,
        SchemaFindings findings,
        List<IntakeError> errors
    )
    {
        var schemaMetrics = schemaNode[PropertiesKeyword.Instance.Name]![MetricSchemaConstants.Fields.Metrics]!;
        var definitions = schemaMetrics[PropertiesKeyword.Instance.Name]!;
        var values = new List<ParsedMetricValue>();

        foreach (var property in metrics.EnumerateObject())
        {
            var path = $"{MetricSchemaConstants.Fields.Metrics}.{property.Name}";

            if (findings.UnknownMetrics.Contains(property.Name))
            {
                errors.Add(Error(path, IntakeErrorCodes.UnknownMetric));

                continue;
            }

            var value = findings.InvalidMetricValues.Contains(property.Name)
                ? null
                : ReadValue(property.Name, definitions[property.Name]!, property.Value);

            if (value is null)
            {
                errors.Add(Error(path, IntakeErrorCodes.WrongType));

                continue;
            }

            values.Add(value);
        }

        var missingMetrics = RequiredNames(schemaMetrics).Where(required => !metrics.TryGetProperty(required, out _));

        errors.AddRange(
            missingMetrics.Select(
                required => Error(
                    $"{MetricSchemaConstants.Fields.Metrics}.{required}",
                    IntakeErrorCodes.MissingMetric)));

        return values;
    }

    private static IEnumerable<string> RequiredNames(JsonNode schemaNode) =>
        schemaNode[RequiredKeyword.Instance.Name]!.AsArray().Select(name => name!.GetValue<string>());

    private static ParsedMetricValue? ReadValue(string key, JsonNode definition, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                if (!value.TryGetDouble(out var number) || !double.IsFinite(number))
                {
                    return null;
                }

                return new ParsedMetricValue
                {
                    Key = key,
                    Number = number,
                    IsOutOfRange = IsOutsideRange(definition[MetricSchemaConstants.Annotations.Range], number),
                };

            case JsonValueKind.True or JsonValueKind.False:
                return new ParsedMetricValue { Key = key, Text = value.GetRawText(), IsOutOfRange = false };

            default:
                var text = value.GetString()!;
                var states = definition[MetricSchemaConstants.Annotations.States]?.AsArray();

                return new ParsedMetricValue
                {
                    Key = key,
                    Text = text,
                    IsOutOfRange = states is not null
                        && states.All(state => !string.Equals(
                            state!.GetValue<string>(), text, StringComparison.Ordinal)),
                };
        }
    }

    private static bool IsOutsideRange(JsonNode? range, double number)
    {
        if (range is null)
        {
            return false;
        }

        var min = range[MetricSchemaConstants.Annotations.RangeMin]?.GetValue<double>();
        var max = range[MetricSchemaConstants.Annotations.RangeMax]?.GetValue<double>();

        return number < min || number > max;
    }

    private static List<string> CheckStatuses(JsonElement statuses, List<IntakeError> errors)
    {
        var codes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in statuses.EnumerateArray().Select(element => element.GetString()!))
        {
            if (!seen.Add(code))
            {
                errors.Add(Error($"{MetricSchemaConstants.Fields.Statuses}.{code}", IntakeErrorCodes.DuplicateStatus));

                continue;
            }

            codes.Add(code);
        }

        return codes;
    }

    private static ParsedMachineMessage EnvelopeFailure(IReadOnlyList<IntakeError> errors) =>
        new() { Errors = errors, IsEnvelopeFailure = true };

    private static IntakeError Error(string path, string code) => new() { Path = path, Code = code };
}
