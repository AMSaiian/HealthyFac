using HealthyFac.Shared.Domain.Enumerations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// Defines the operators of a phase condition. Each operator belongs to one kind of source: a status, or a metric of
/// one value kind. The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class PhaseConditionOperator : Enumeration<PhaseConditionOperator>
{
    public static readonly PhaseConditionOperator Active = new(value: "active", isForStatus: true, kind: null);
    public static readonly PhaseConditionOperator Inactive = new(value: "inactive", isForStatus: true, kind: null);

    public static readonly PhaseConditionOperator GreaterThan =
        new(value: "greaterThan", isForStatus: false, MetricValueKind.Numeric);

    public static readonly PhaseConditionOperator GreaterOrEqual =
        new(value: "greaterOrEqual", isForStatus: false, MetricValueKind.Numeric);

    public static readonly PhaseConditionOperator LessThan =
        new(value: "lessThan", isForStatus: false, MetricValueKind.Numeric);

    public static readonly PhaseConditionOperator LessOrEqual =
        new(value: "lessOrEqual", isForStatus: false, MetricValueKind.Numeric);

    public static readonly PhaseConditionOperator Between =
        new(value: "between", isForStatus: false, MetricValueKind.Numeric);

    public static readonly PhaseConditionOperator IsTrue =
        new(value: "isTrue", isForStatus: false, MetricValueKind.Boolean);

    public static readonly PhaseConditionOperator IsFalse =
        new(value: "isFalse", isForStatus: false, MetricValueKind.Boolean);

    public static readonly PhaseConditionOperator In =
        new(value: "in", isForStatus: false, MetricValueKind.StateSet);

    private readonly bool _isForStatus;
    private readonly MetricValueKind? _kind;

    private PhaseConditionOperator(string value, bool isForStatus, MetricValueKind? kind)
        : base(value)
    {
        _isForStatus = isForStatus;
        _kind = kind;
    }

    /// <summary>Tells whether the operator needs one comparison value (<c>Value</c>).</summary>
    public bool HasComparisonValue => this == GreaterThan
        || this == GreaterOrEqual
        || this == LessThan
        || this == LessOrEqual
        || this == Between;

    /// <summary>
    /// Tells whether the operator fits a source: a status, or a metric of the value kind of the operator.
    /// </summary>
    /// <param name="source">The source of the condition.</param>
    /// <returns><see langword="true"/> when the operator fits the source.</returns>
    public bool Accepts(PhaseConditionSource source) =>
        source.StatusDefinitionId is not null ? _isForStatus : source.ValueKind == _kind;
}
