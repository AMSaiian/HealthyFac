using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Turns the answer of the model into the <see cref="DraftsProposed"/> message. The model is untrusted: every field
/// that breaks a rule of its target becomes <see langword="null"/>, a list over its cap is cut with a warning, and
/// the finished message must pass <see cref="DraftsProposedValidator"/>.
/// </summary>
/// <remarks>
/// Rules: a machine file keeps its first machine, with the machine of the file as target. A key or a code that is too
/// long or breaks its pattern, a name or a serial number over its limit, a quantity kind and unit pair outside the
/// catalog (or a <c>Discrete</c> pair), a range with <c>Min</c> not below <c>Max</c>, a range on a metric that is not
/// numeric, and states on a metric that is not a state set become null. A description becomes plain paragraphs in
/// HTML, and null when the rich text policy refuses it. A duplicate key or code inside one machine keeps the first.
/// An entry without any value is dropped.
/// </remarks>
public sealed partial class DraftProposalMapper
{
    private const char Space = ' ';
    private const char UnitKeySeparator = (char)31;
    private const string ValidationFailureFormat = "The mapped drafts fail the message rules: {0}";

    private readonly IRichTextPolicy _richTextPolicy;
    private readonly DraftsProposedValidator _validator;

    public DraftProposalMapper(IRichTextPolicy richTextPolicy)
    {
        _richTextPolicy = richTextPolicy;
        _validator = new DraftsProposedValidator(richTextPolicy);
    }

    /// <summary>
    /// Maps the extraction.
    /// </summary>
    /// <param name="result">The answer of the model.</param>
    /// <param name="context">The attempt, the file, and the unit catalog.</param>
    /// <returns>The message (or <see langword="null"/> for 0 entries), the draft count, and the warning.</returns>
    /// <exception cref="InvalidExtractionException">Thrown when the mapped message fails its validator.</exception>
    public MappingResult Map(ExtractionResult result, MappingContext context)
    {
        var warnings = new List<string>();
        var units = BuildUnitLookup(context.Units);
        var source = context.IsMachineFile ? result.Machines.Take(count: 1).ToList() : result.Machines.ToList();
        var machines = source
            .Select(machine => MapMachine(machine, context, units, warnings))
            .OfType<ProposedMachine>()
            .ToList();

        if (machines.Count > DraftMessageLimits.MaxMachines)
        {
            warnings.Add(
                ExtractionWarnings.Truncated(
                    ExtractionWarnings.MachinesKind,
                    machines.Count,
                    DraftMessageLimits.MaxMachines));
            machines = machines.Take(DraftMessageLimits.MaxMachines).ToList();
        }

        if (machines.Count == 0)
        {
            return new MappingResult { DraftCount = 0, Warning = warnings.FirstOrDefault() };
        }

        var message = new DraftsProposed
        {
            AttemptId = context.AttemptId,
            FileId = context.FileId,
            CompanyId = context.CompanyId,
            IsMachineFile = context.IsMachineFile,
            QueuedAt = context.QueuedAt,
            Machines = machines,
        };
        EnsureValid(message);

        return new MappingResult
        {
            Message = message,
            DraftCount = machines.Count + machines.Sum(machine => machine.Metrics.Count + machine.Statuses.Count),
            Warning = warnings.FirstOrDefault(),
        };
    }

    private static Dictionary<string, MetricUnit> BuildUnitLookup(IReadOnlyList<MetricUnit> units) =>
        units
            .Where(unit => !unit.IsDiscrete)
            .GroupBy(unit => UnitKey(unit.QuantityKind, unit.Name))
            .ToDictionary(group => group.Key, group => group.First());

    private static string UnitKey(string kind, string name) =>
        string.Concat(kind.ToUpperInvariant(), UnitKeySeparator, name.ToUpperInvariant());

    private static string? SingleLine(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? Space : character);
        }

        var cleaned = WhitespacePattern().Replace(TextInputPolicy.Normalize(builder.ToString()), Space.ToString());

        return cleaned.Length == 0 || cleaned.Length > maxLength ? null : cleaned;
    }

    private static string? Location(string? value)
    {
        var cleaned = SingleLine(value, maxLength: int.MaxValue);

        return cleaned is null || cleaned.Length <= DraftMessageLimits.SourceLocationMaxLength
            ? cleaned
            : cleaned[..DraftMessageLimits.SourceLocationMaxLength];
    }

    private static double? FiniteOrNull(double? value) =>
        value is { } number && double.IsFinite(number) ? number : null;

    private static string? ValueKindText(ExtractedValueKind? kind) =>
        kind switch
        {
            ExtractedValueKind.Numeric => DraftMessageLimits.ValueKinds.Numeric,
            ExtractedValueKind.Boolean => DraftMessageLimits.ValueKinds.Boolean,
            ExtractedValueKind.StateSet => DraftMessageLimits.ValueKinds.StateSet,
            _ => null,
        };

    private static string? SeverityText(ExtractedSeverity? severity) =>
        severity switch
        {
            ExtractedSeverity.Info => DraftMessageLimits.Severities.Info,
            ExtractedSeverity.Warning => DraftMessageLimits.Severities.Warning,
            ExtractedSeverity.Error => DraftMessageLimits.Severities.Error,
            _ => null,
        };

    private static List<string>? MapStates(IReadOnlyList<string>? states, ExtractedValueKind? kind)
    {
        if (states is null || kind is not null and not ExtractedValueKind.StateSet)
        {
            return null;
        }

        var unique = states
            .Select(state => SingleLine(state, DraftMessageLimits.Metric.StateMaxLength))
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return unique.Count >= DraftMessageLimits.MinStates ? unique : null;
    }

    private static (double? Min, double? Max) MapRange(ExtractedMetric metric)
    {
        var isNotNumeric = metric.ValueKind is not null and not ExtractedValueKind.Numeric;
        var min = FiniteOrNull(metric.Min);
        var max = FiniteOrNull(metric.Max);
        if (isNotNumeric || (min is not null && max is not null && min >= max))
        {
            return (null, null);
        }

        return (min, max);
    }

    private static void KeepFirst<T>(ref List<T> items, Func<T, string?> identity)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        items = items.Where(item => identity(item) is not { } value || seen.Add(value)).ToList();
    }

    private static void Cap<T>(ref List<T> items, int cap, string kind, List<string> warnings)
    {
        if (items.Count <= cap)
        {
            return;
        }

        warnings.Add(ExtractionWarnings.Truncated(kind, items.Count, cap));
        items = items.Take(cap).ToList();
    }

    private ProposedMachine? MapMachine(
        ExtractedMachine machine,
        MappingContext context,
        Dictionary<string, MetricUnit> units,
        List<string> warnings
    )
    {
        var metrics = machine.Metrics
            .Select(metric => MapMetric(metric, units))
            .OfType<ProposedMetric>()
            .ToList();
        var statuses = machine.Statuses.Select(MapStatus).OfType<ProposedStatus>().ToList();
        KeepFirst(ref metrics, metric => metric.Key);
        KeepFirst(ref statuses, status => status.Code);
        Cap(ref metrics, DraftMessageLimits.MaxMetrics, ExtractionWarnings.MetricsKind, warnings);
        Cap(ref statuses, DraftMessageLimits.MaxStatuses, ExtractionWarnings.StatusesKind, warnings);

        var name = SingleLine(machine.Name, DraftMessageLimits.Machine.NameMaxLength);
        var serialNumber = SingleLine(machine.SerialNumber, DraftMessageLimits.Machine.SerialNumberMaxLength);
        var description = MapDescription(machine.Description);
        var hasContent = name is not null
            || serialNumber is not null
            || description is not null
            || metrics.Count > 0
            || statuses.Count > 0;

        return hasContent
            ? new ProposedMachine
            {
                DraftId = Guid.CreateVersion7(),
                TargetMachineId = context.IsMachineFile ? context.FileMachineId : null,
                Name = name,
                SerialNumber = serialNumber,
                Description = description,
                Metrics = metrics,
                Statuses = statuses,
            }
            : null;
    }

    private ProposedMetric? MapMetric(ExtractedMetric metric, Dictionary<string, MetricUnit> units)
    {
        var key = SingleLine(metric.Key, DraftMessageLimits.Metric.KeyMaxLength);
        var unit = MapUnit(metric, units);
        var (min, max) = MapRange(metric);
        var mapped = new ProposedMetric
        {
            DraftId = Guid.CreateVersion7(),
            SourceLocation = Location(metric.SourceLocation),
            Key = key is not null && KeyPattern().IsMatch(key) ? key : null,
            Name = SingleLine(metric.Name, DraftMessageLimits.Metric.NameMaxLength),
            Description = MapDescription(metric.Description),
            ValueKind = ValueKindText(metric.ValueKind),
            QuantityKind = unit?.QuantityKind,
            Unit = unit?.Name,
            Min = min,
            Max = max,
            States = MapStates(metric.States, metric.ValueKind),
        };

        var hasValue = mapped.Key is not null
            || mapped.Name is not null
            || mapped.Description is not null
            || mapped.ValueKind is not null
            || mapped.QuantityKind is not null
            || mapped.Min is not null
            || mapped.Max is not null
            || mapped.States is not null;

        return hasValue ? mapped : null;
    }

    private ProposedStatus? MapStatus(ExtractedStatus status)
    {
        var mapped = new ProposedStatus
        {
            DraftId = Guid.CreateVersion7(),
            SourceLocation = Location(status.SourceLocation),
            Code = SingleLine(status.Code, DraftMessageLimits.Status.CodeMaxLength),
            Severity = SeverityText(status.Severity),
            Description = MapDescription(status.Description),
        };

        return mapped.Code is not null || mapped.Severity is not null || mapped.Description is not null
            ? mapped
            : null;
    }

    private static MetricUnit? MapUnit(ExtractedMetric metric, Dictionary<string, MetricUnit> units)
    {
        var kind = SingleLine(metric.QuantityKind, DraftMessageLimits.Metric.UnitNameMaxLength);
        var name = SingleLine(metric.Unit, DraftMessageLimits.Metric.UnitNameMaxLength);
        if (kind is null || name is null)
        {
            return null;
        }

        return units.GetValueOrDefault(UnitKey(kind, name));
    }

    private string? MapDescription(string? text)
    {
        var normalized = text is null ? null : TextInputPolicy.Normalize(text);
        var html = PlainTextHtml.FromPlainText(normalized);
        if (html is null || html.Length > RichTextConstants.HtmlMaxLength || html.Any(IsForbiddenControl))
        {
            return null;
        }

        var inspection = _richTextPolicy.Inspect(html);

        return inspection.IsAllowed && inspection.VisibleLength <= DraftMessageLimits.DescriptionMaxLength
            ? html
            : null;
    }

    private static bool IsForbiddenControl(char character) =>
        char.IsControl(character) && character is not '\n' and not '\t';

    private void EnsureValid(DraftsProposed message)
    {
        var validation = _validator.Validate(message);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage).Distinct());

            throw new InvalidExtractionException(string.Format(ValidationFailureFormat, errors));
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(DraftMessageLimits.KeyPattern)]
    private static partial Regex KeyPattern();
}
