using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

public static class StateEffectTestData
{
    public const string PhaseCode = "cutting";

    public static TheoryData<string, MachineStateValue> BlockedStates => new()
    {
        { "in maintenance", MachineStateValue.InMaintenance },
        { "out of service", MachineStateValue.OutOfService },
    };

    /// <summary>Case name, the state that the machine passes through, and whether the way back resets.</summary>
    public static TheoryData<string, MachineStateValue, bool> StateRoundTrips => new()
    {
        { "the end of maintenance", MachineStateValue.InMaintenance, true },
        { "the end of out of service", MachineStateValue.OutOfService, false },
    };

    /// <summary>Case name, the change of the phase body, and whether the statistics of the phase restart.</summary>
    public static TheoryData<
        string,
        Func<UpdateOperatingPhaseRequest, EvaluationArrangement, UpdateOperatingPhaseRequest>,
        bool> PhaseChanges => new()
    {
        {
            "a condition change",
            (request, arrangement) => request with
            {
                Conditions =
                [
                    new PhaseConditionInput
                    {
                        StatusDefinitionId = arrangement.PartChangeStatusId,
                        Operator = PhaseConditionOperator.Active,
                    },
                ],
            },
            true
        },
        { "an isTracked change", (request, _) => request with { IsTracked = false }, true },
        {
            "a limit row change",
            (request, arrangement) => request with
            {
                Limits =
                [
                    new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 95 },
                ],
            },
            false
        },
        { "a name change", (request, _) => request with { Name = "Cutting phase" }, false },
    };

    public static UpdateOperatingPhaseRequest UpdateRequest(
        EvaluationArrangement arrangement,
        Guid phaseId,
        Guid companyId
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = arrangement.MachineId,
            Id = phaseId,
            Code = PhaseCode,
            Name = PhaseCode,
            Priority = 0,
            IsTracked = true,
            Conditions =
            [
                new PhaseConditionInput
                {
                    StatusDefinitionId = arrangement.CuttingStatusId,
                    Operator = PhaseConditionOperator.Active,
                },
            ],
            Limits = [],
        };
}
