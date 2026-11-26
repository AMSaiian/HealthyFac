using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.UnitTests.OperatingPhases.Data;

public static class OperatingPhaseTestData
{
    public static readonly Guid RegistrationId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    public static readonly Guid StatusId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b1");
    public static readonly Guid NumericMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b2");
    public static readonly Guid OtherNumericMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b3");
    public static readonly Guid BooleanMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b4");
    public static readonly Guid StateMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b5");

    public static readonly IReadOnlyList<string> States = ["off", "idle", "running"];

    public static readonly PhaseConditionSource StatusSource = PhaseConditionSource.Status(StatusId);

    public static readonly PhaseConditionSource NumericSource =
        PhaseConditionSource.Metric(NumericMetricId, MetricValueKind.Numeric, states: null);

    public static readonly PhaseConditionSource BooleanSource =
        PhaseConditionSource.Metric(BooleanMetricId, MetricValueKind.Boolean, states: null);

    public static readonly PhaseConditionSource StateSource =
        PhaseConditionSource.Metric(StateMetricId, MetricValueKind.StateSet, States);

    public static TheoryData<string, PhaseConditionOperator, PhaseConditionSource, bool> OperatorsAgainstSources()
    {
        var sources = new (string Name, PhaseConditionSource Source, string? Kind)[]
        {
            ("status", StatusSource, null),
            ("numeric metric", NumericSource, MetricValueKind.Numeric.Value),
            ("boolean metric", BooleanSource, MetricValueKind.Boolean.Value),
            ("state set metric", StateSource, MetricValueKind.StateSet.Value),
        };

        var operatorKinds = new Dictionary<PhaseConditionOperator, string?>
        {
            [PhaseConditionOperator.Active] = null,
            [PhaseConditionOperator.Inactive] = null,
            [PhaseConditionOperator.GreaterThan] = MetricValueKind.Numeric.Value,
            [PhaseConditionOperator.GreaterOrEqual] = MetricValueKind.Numeric.Value,
            [PhaseConditionOperator.LessThan] = MetricValueKind.Numeric.Value,
            [PhaseConditionOperator.LessOrEqual] = MetricValueKind.Numeric.Value,
            [PhaseConditionOperator.Between] = MetricValueKind.Numeric.Value,
            [PhaseConditionOperator.IsTrue] = MetricValueKind.Boolean.Value,
            [PhaseConditionOperator.IsFalse] = MetricValueKind.Boolean.Value,
            [PhaseConditionOperator.In] = MetricValueKind.StateSet.Value,
        };

        var data = new TheoryData<string, PhaseConditionOperator, PhaseConditionSource, bool>();

        foreach (var (conditionOperator, operatorKind) in operatorKinds)
        {
            foreach (var (name, source, sourceKind) in sources)
            {
                data.Add($"{conditionOperator.Value} on {name}", conditionOperator, source, operatorKind == sourceKind);
            }
        }

        return data;
    }

    public static TheoryData<string, double, double> BetweenValuesNotAscending => new()
    {
        { "equal values", 10, 10 },
        { "lower value above upper value", 20, 10 },
    };

    public static TheoryData<string, IReadOnlyList<string>?> InvalidStates => new()
    {
        { "a state that the metric does not define", ["running", "unknown"] },
        { "no state", [] },
        { "null states", null },
    };

    public static TheoryData<string, Func<OperatingPhaseCondition[]>, bool> ConditionReplacements => new()
    {
        { "the same condition", () => [CreateStatusCondition()], false },
        {
            "another operator on the same status",
            () =>
            [
                OperatingPhaseCondition.Create(
                    StatusSource, PhaseConditionOperator.Inactive, value: null, upperValue: null, states: null),
            ],
            true
        },
        { "one more condition", () => [CreateStatusCondition(), CreateNumericCondition()], true },
    };

    public static OperatingPhaseCondition CreateNumericCondition() =>
        OperatingPhaseCondition.Create(
            NumericSource,
            PhaseConditionOperator.GreaterThan,
            value: 10,
            upperValue: null,
            states: null);

    public static OperatingPhaseCondition CreateStatusCondition() =>
        OperatingPhaseCondition.Create(
            StatusSource,
            PhaseConditionOperator.Active,
            value: null,
            upperValue: null,
            states: null);

    public static OperatingPhaseLimit CreateLimit(Guid metricId) =>
        OperatingPhaseLimit.Create(metricId, MetricValueKind.Numeric, new MetricLimits { AlarmHigh = 90 });

    public static OperatingPhase CreatePhase(
        IReadOnlyList<OperatingPhaseCondition> conditions,
        IReadOnlyList<OperatingPhaseLimit> limits,
        bool isTracked = true
    ) =>
        new(
            RegistrationId,
            code: "cutting",
            name: "Cutting",
            description: null,
            priority: 0,
            isTracked,
            conditions,
            limits);
}
