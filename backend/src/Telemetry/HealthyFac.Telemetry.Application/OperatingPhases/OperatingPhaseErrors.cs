using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// Defines error messages for operating phase operations.
/// </summary>
public static class OperatingPhaseErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string PhaseIdRequired = "Phase ID is required.";
    public const string PhaseNotFound = "The operating phase does not exist.";
    public const string CodeRequired = "Phase code is required.";
    public const string CodeInvalid = "Phase code must be a lower case key with dots, for example 'part_change'.";
    public const string NameRequired = "Phase name is required.";
    public const string PriorityNegative = "Priority must be at least 0.";

    public const string CodeOrPriorityTaken =
        "A phase with this code or this priority already exists for this machine.";

    public const string PhaseChanged = "The phase changed in the meantime. Reload it and try again.";
    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";
    public const string ConditionsRequired = "A phase needs at least 1 condition.";
    public const string ConditionSourceInvalid = "A condition needs exactly one source: a metric or a status.";
    public const string OperatorRequired = "Operator is required.";
    public const string ValueRequired = "This operator needs a value.";
    public const string UpperValueRequired = "The operator 'between' needs an upper value.";
    public const string ValuesNotAscending = "The value must be below the upper value.";
    public const string StatesRequired = "The operator 'in' needs at least 1 state.";
    public const string SourceUnknown = "The metric or status does not exist for this machine.";
    public const string OperatorDoesNotFit = "The operator does not fit the kind of the source.";
    public const string StateUnknown = "The metric does not define this state.";
    public const string LimitMetricIdRequired = "The limit needs a metric.";
    public const string LimitMetricUnknown = "The metric of the limit does not exist for this machine.";
    public const string LimitMetricNotNumeric = "A limit needs a numeric metric.";
    public const string LimitMetricDuplicated = "A metric has more than 1 limit row.";
    public const string LimitOnConditionMetric = "A limit row cannot use a metric that a condition of the phase uses.";
    public const string WarningLowBelowAlarmLow = "The warning low limit must not be below the alarm low limit.";
    public const string WarningHighAboveAlarmHigh = "The warning high limit must not be above the alarm high limit.";
    public const string PhaseIdsInvalid = "The list must hold the ID of each phase of the machine exactly once.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {OperatingPhaseValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
