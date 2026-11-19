using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

public static class OperatingPhaseTestData
{
    public const string StatusCode = "S002";
    public const string StateMetricKey = "spindle.mode";

    public const string CuttingCode = "cutting";
    public const string StoppedCode = "stopped";
    public const string IdleCode = "idle";
    public const string PhaseIdsKey = "phaseIds";

    public static readonly IReadOnlyList<string> StateMetricStates = ["off", "running"];

    public static readonly MetricLimits PhaseLimits = new() { WarningHigh = 80, AlarmHigh = 90 };

    public static TheoryData<string, Func<PhaseArrangement, CreateOperatingPhaseRequest>> InvalidBodies => new()
    {
        {
            "a condition on an unknown source",
            arrangement => CreateRequest(arrangement) with
            {
                Conditions = [StatusCondition(Guid.CreateVersion7())],
            }
        },
        {
            "an operator that does not fit a status",
            arrangement => CreateRequest(arrangement) with
            {
                Conditions = [StatusCondition(arrangement.StatusId) with { Operator = PhaseConditionOperator.IsTrue }],
            }
        },
        {
            "a state that the metric does not define",
            arrangement => CreateRequest(arrangement) with
            {
                Conditions =
                [
                    new PhaseConditionInput
                    {
                        MetricDefinitionId = arrangement.StateMetricId,
                        Operator = PhaseConditionOperator.In,
                        States = ["unknown"],
                    },
                ],
            }
        },
        {
            "a limit row on an unknown metric",
            arrangement => CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = Guid.CreateVersion7(), AlarmHigh = 1 }],
            }
        },
        {
            "a limit row on a state set metric",
            arrangement => CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.StateMetricId, AlarmHigh = 1 }],
            }
        },
        {
            "a limit row on a metric that a condition uses",
            arrangement => CreateRequest(arrangement) with
            {
                Conditions =
                [
                    new PhaseConditionInput
                    {
                        MetricDefinitionId = arrangement.MetricId,
                        Operator = PhaseConditionOperator.GreaterThan,
                        Value = 10,
                    },
                ],
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 1 }],
            }
        },
        {
            "a warning low limit below the alarm low limit",
            arrangement => CreateRequest(arrangement) with
            {
                Limits =
                [
                    new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, WarningLow = 1, AlarmLow = 5 },
                ],
            }
        },
    };

    public static CreateOperatingPhaseRequest CreateRequest(
        PhaseArrangement arrangement,
        string code = CuttingCode,
        int priority = 0
    ) =>
        new()
        {
            CompanyId = MachineRegistrationTestData.CompanyId,
            MachineId = arrangement.MachineId,
            Code = code,
            Name = code,
            Priority = priority,
            IsTracked = true,
            Conditions = [StatusCondition(arrangement.StatusId)],
            Limits = [],
        };

    public static PhaseConditionInput StatusCondition(Guid statusId) =>
        new() { StatusDefinitionId = statusId, Operator = PhaseConditionOperator.Active };

    public static OperatingPhase CreatePhase(PhaseArrangement arrangement, string code, int priority, bool isTracked)
    {
        var condition = OperatingPhaseCondition.Create(
            PhaseConditionSource.Status(arrangement.StatusId),
            PhaseConditionOperator.Active,
            value: null,
            upperValue: null,
            states: null);

        return new OperatingPhase(
            arrangement.MachineId,
            code,
            code,
            description: null,
            priority,
            isTracked,
            [condition],
            [OperatingPhaseLimit.Create(arrangement.MetricId, MetricValueKind.Numeric, PhaseLimits)]);
    }

    public static FormattableString InsertConditionWithBothSources(Guid phaseId, Guid metricId, Guid statusId) =>
        $"""
         INSERT INTO operating_phase_conditions
             (id, phase_id, metric_definition_id, status_definition_id, operator, created_at, created_by)
         VALUES ({Guid.CreateVersion7()}, {phaseId}, {metricId}, {statusId}, 'active', now(), {Guid.CreateVersion7()})
         """;

    public static FormattableString InsertConditionWithNoSource(Guid phaseId) =>
        $"""
         INSERT INTO operating_phase_conditions (id, phase_id, operator, created_at, created_by)
         VALUES ({Guid.CreateVersion7()}, {phaseId}, 'active', now(), {Guid.CreateVersion7()})
         """;
}
