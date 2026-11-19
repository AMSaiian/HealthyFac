using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.UnitTests.Evaluation.Data;

public static class PhaseMatcherTestData
{
    public static readonly Guid MetricId = Guid.Parse(input: "00000000-0000-7000-8000-000000000001");
    public static readonly Guid FlagId = Guid.Parse(input: "00000000-0000-7000-8000-000000000002");
    public static readonly Guid ModeId = Guid.Parse(input: "00000000-0000-7000-8000-000000000003");
    public static readonly Guid StatusId = Guid.Parse(input: "00000000-0000-7000-8000-000000000004");

    public static readonly PhaseSettings Settings = new() { PhaseDwellSeconds = 15, SettleTimeSeconds = 30 };

    private static readonly string[] Modes = ["idle", "run"];

    private static readonly PhaseConditionSource Numeric =
        PhaseConditionSource.Metric(MetricId, MetricValueKind.Numeric, states: null);

    private static readonly PhaseConditionSource Flag =
        PhaseConditionSource.Metric(FlagId, MetricValueKind.Boolean, states: null);

    private static readonly PhaseConditionSource Mode =
        PhaseConditionSource.Metric(ModeId, MetricValueKind.StateSet, Modes);

    private static readonly PhaseConditionSource Status = PhaseConditionSource.Status(StatusId);

    /// <summary>Case name, the condition, the sample that makes it hold, and the sample that makes it fail.</summary>
    public static TheoryData<string, OperatingPhaseCondition, SampleValues, SampleValues> OperatorCases => new()
    {
        {
            "greaterThan", Condition(Numeric, PhaseConditionOperator.GreaterThan, value: 10), WithNumber(value: 11),
            WithNumber(value: 10)
        },
        {
            "greaterOrEqual", Condition(Numeric, PhaseConditionOperator.GreaterOrEqual, value: 10),
            WithNumber(value: 10),
            WithNumber(value: 9)
        },
        {
            "lessThan", Condition(Numeric, PhaseConditionOperator.LessThan, value: 10), WithNumber(value: 9),
            WithNumber(value: 10)
        },
        {
            "lessOrEqual", Condition(Numeric, PhaseConditionOperator.LessOrEqual, value: 10), WithNumber(value: 10),
            WithNumber(value: 11)
        },
        {
            "between at the lower edge", Condition(Numeric, PhaseConditionOperator.Between, value: 10, upper: 20),
            WithNumber(value: 10), WithNumber(value: 9)
        },
        {
            "between at the upper edge", Condition(Numeric, PhaseConditionOperator.Between, value: 10, upper: 20),
            WithNumber(value: 20), WithNumber(value: 21)
        },
        {
            "isTrue", Condition(Flag, PhaseConditionOperator.IsTrue), WithDiscrete(FlagId, value: "true"),
            WithDiscrete(FlagId, value: "false")
        },
        {
            "isFalse", Condition(Flag, PhaseConditionOperator.IsFalse), WithDiscrete(FlagId, value: "false"),
            WithDiscrete(FlagId, value: "true")
        },
        {
            "in", Condition(Mode, PhaseConditionOperator.In, states: ["run"]), WithDiscrete(ModeId, value: "run"),
            WithDiscrete(ModeId, value: "idle")
        },
        {
            "active", Condition(Status, PhaseConditionOperator.Active), WithStatus(active: true),
            WithStatus(active: false)
        },
        {
            "inactive", Condition(Status, PhaseConditionOperator.Inactive), WithStatus(active: false),
            WithStatus(active: true)
        },
    };

    /// <summary>The conditions on a metric that the sample does not carry.</summary>
    public static TheoryData<string, OperatingPhaseCondition> MissingValueCases => new()
    {
        { "numeric", Condition(Numeric, PhaseConditionOperator.LessThan, value: 10) },
        { "boolean", Condition(Flag, PhaseConditionOperator.IsFalse) },
        { "state set", Condition(Mode, PhaseConditionOperator.In, states: ["run"]) },
    };

    public static OperatingPhase Phase(string code, int priority, params OperatingPhaseCondition[] conditions) =>
        new(
            Guid.CreateVersion7(),
            code,
            code,
            description: null,
            priority,
            isTracked: true,
            conditions,
            []);

    public static OperatingPhaseCondition Condition(
        PhaseConditionSource source,
        PhaseConditionOperator conditionOperator,
        double? value = null,
        double? upper = null,
        string[]? states = null
    ) =>
        OperatingPhaseCondition.Create(source, conditionOperator, value, upper, states);

    public static OperatingPhaseCondition NumericAbove(double value) =>
        Condition(Numeric, PhaseConditionOperator.GreaterThan, value);

    public static SampleValues WithNumber(double value) =>
        new() { Numeric = new Dictionary<Guid, double> { [MetricId] = value } };

    public static SampleValues WithDiscrete(Guid metricId, string value) =>
        new() { Discrete = new Dictionary<Guid, string> { [metricId] = value } };

    public static SampleValues WithStatus(bool active) =>
        new() { ActiveStatusIds = active ? new HashSet<Guid> { StatusId } : new HashSet<Guid>() };
}
