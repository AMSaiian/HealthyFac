using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class PhaseDraftApplyTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyDefinitions_ThatAPhaseDraftUses_MoveItsRowsFromTheDraftsToTheNewDefinitions()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);

        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric() with { DraftId = ids.MetricId }]);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, code: "E104", draftId: ids.StatusId);

        var metrics = await _fixture.LoadMetricRowsAsync(machineId);
        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        var condition = phase.Conditions.Single();
        var limit = phase.Limits.Single();
        condition.StatusDefinitionId.Should().Be(statusId);
        condition.StatusDraftId.Should().BeNull();
        limit.MetricDefinitionId.Should().Be(metrics.Single().Id);
        limit.MetricDraftId.Should().BeNull();
        phase.HasUnresolvedTargets.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPhaseDraft_WhileARowStillNamesADraft_Returns422AndCreatesNothing()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);
        var statusId = await _fixture.CreateStatusDefinitionAsync(tenant.CompanyId, machineId, code: "E104");

        var act = () => _fixture.ApplyPhaseDraftAsync(PhaseApplyRequest(tenant.CompanyId, machineId, ids, statusId));

        var exception = await act.Should().ThrowAsync<UnprocessableException>();

        var phases = await _fixture.LoadPhasesOfMachineAsync(machineId);
        var draft = (await _fixture.LoadPhaseDraftsAsync()).Single();
        exception.Which.Message.Should().Be(DefinitionDraftErrors.DraftUsesDrafts);
        phases.Should().BeEmpty();
        draft.State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task ApplyPhaseDraft_AfterItsDraftsAreApplied_CreatesThePhaseAndMarksTheDraftApplied()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);
        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric() with { DraftId = ids.MetricId }]);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, machineId, code: "E104", draftId: ids.StatusId);

        var phaseId = await _fixture.ApplyPhaseDraftAsync(
            PhaseApplyRequest(tenant.CompanyId, machineId, ids, statusId));

        var phases = await _fixture.LoadPhasesOfMachineAsync(machineId);
        var draft = (await _fixture.LoadPhaseDraftsAsync()).Single();
        phases.Should().ContainSingle().Which.Id.Should().Be(phaseId);
        phases.Single().Conditions.Should().ContainSingle().Which.StatusDefinitionId.Should().Be(statusId);
        draft.State.Should().Be(DefinitionDraftState.Applied);
    }

    [Theory]
    [MemberData(nameof(PhaseDraftTestData.UsedDrafts), MemberType = typeof(PhaseDraftTestData))]
    public async Task DismissDraft_ThatAPendingPhaseDraftUses_Returns422AndDismissesNothing(
        string caseName,
        Func<FlowIds, Guid> usedDraftId
    )
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);

        var act = () => _fixture.DismissDefinitionDraftAsync(tenant.CompanyId, usedDraftId(ids));

        var exception = await act.Should().ThrowAsync<UnprocessableException>(caseName);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();
        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        exception.Which.Message.Should().Be(DefinitionDraftErrors.DraftUsedByPhaseDraft);
        metric.State.Should().Be(DefinitionDraftState.Pending);
        status.State.Should().Be(DefinitionDraftState.Pending);
        phase.State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task DismissDraft_ThatOnlyADismissedPhaseDraftUses_DismissesIt()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);
        await _fixture.DismissDefinitionDraftAsync(tenant.CompanyId, ids.PhaseId);

        await _fixture.DismissDefinitionDraftAsync(tenant.CompanyId, ids.MetricId);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        metric.State.Should().Be(DefinitionDraftState.Dismissed);
        phase.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    [Fact]
    public async Task DismissDrafts_OfAMachine_DismissesThePhaseDraftsToo()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);

        var response = await _fixture.DismissDefinitionDraftsAsync(tenant.CompanyId, machineId, attemptId: null);

        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        response.DismissedCount.Should().Be(expected: 3);
        phase.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    private static ApplyOperatingPhaseDraftRequest PhaseApplyRequest(
        Guid companyId,
        Guid machineId,
        FlowIds ids,
        Guid statusId
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = machineId,
            Id = ids.PhaseId,
            Code = PhaseDraftTestData.PhaseCode,
            Name = "Cutting",
            Priority = 0,
            IsTracked = true,
            Conditions =
                [new PhaseConditionInput { StatusDefinitionId = statusId, Operator = PhaseConditionOperator.Active }],
            Limits = [],
        };
}
