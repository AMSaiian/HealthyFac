using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class ProposalEntryFlowTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ProposalEntryFlowTestData.ApplyOrders), MemberType = typeof(ProposalEntryFlowTestData))]
    public async Task Flow_WithAppliedMachineDraft_EndsPendingOnTheAppliedMachineInAnyOrder(
        string caseName,
        FlowDelivery[] deliveries
    )
    {
        var ids = FlowIds.Create();

        foreach (var delivery in deliveries)
        {
            await delivery(_fixture, ids);
        }

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();

        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Pending, caseName);
        metric.TargetMachineId.Should().Be(ids.MachineId);
        metric.TargetMachineDraftId.Should().BeNull();
        status.State.Should().Be(DefinitionDraftState.Pending, caseName);
        status.TargetMachineId.Should().Be(ids.MachineId);
        phase.State.Should().Be(DefinitionDraftState.Pending, caseName);
        phase.TargetMachineId.Should().Be(ids.MachineId);
    }

    [Theory]
    [MemberData(nameof(ProposalEntryFlowTestData.DismissOrders), MemberType = typeof(ProposalEntryFlowTestData))]
    public async Task Flow_WithDismissedMachineDraft_EndsDismissedInAnyOrder(
        string caseName,
        FlowDelivery[] deliveries
    )
    {
        var ids = FlowIds.Create();

        foreach (var delivery in deliveries)
        {
            await delivery(_fixture, ids);
        }

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();

        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Dismissed, caseName);
        status.State.Should().Be(DefinitionDraftState.Dismissed, caseName);
        phase.State.Should().Be(DefinitionDraftState.Dismissed, caseName);
    }

    [Theory]
    [MemberData(nameof(ProposalEntryFlowTestData.RejectOrders), MemberType = typeof(ProposalEntryFlowTestData))]
    public async Task Flow_WithRejectedProposal_LeavesNoActiveDraftInAnyOrder(
        string caseName,
        FlowDelivery[] deliveries,
        int expectedRows
    )
    {
        var ids = FlowIds.Create();

        foreach (var delivery in deliveries)
        {
            await delivery(_fixture, ids);
        }

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        var phases = await _fixture.LoadPhaseDraftsAsync();

        metrics.Should().HaveCount(expectedRows, caseName);
        statuses.Should().HaveCount(expectedRows, caseName);
        phases.Should().HaveCount(expectedRows, caseName);

        metrics.Concat<IDefinitionDraft>(statuses)
            .Concat(phases)
            .Should()
            .NotContain(draft => !IsRemoved(draft));
    }

    [Theory]
    [MemberData(nameof(ProposalEntryFlowTestData.RepeatedMessages), MemberType = typeof(ProposalEntryFlowTestData))]
    public async Task Flow_WithAMessageDeliveredTwice_GivesTheSameRows(string caseName, FlowDelivery repeated)
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);
        await repeated(_fixture, ids);
        var before = await _fixture.LoadMetricDraftsAsync();

        await repeated(_fixture, ids);

        var after = await _fixture.LoadMetricDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        after.Should().ContainSingle(caseName);
        after.Single().State.Should().Be(before.Single().State);
        after.Single().TargetMachineId.Should().Be(before.Single().TargetMachineId);
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Flow_WithProposalAndAppliedMachineDraftInParallel_ConflictsOnceAndEndsPendingOnTheMachine()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.AcceptedOnMachineDraft(_fixture, ids);
        using var race = _fixture.SaveGate.StartRace(participantCount: 2);

        var failures = await Task.WhenAll(
            race.RunAsync(() => ProposalEntryFlowTestData.Proposed(_fixture, ids)),
            race.RunAsync(() => ProposalEntryFlowTestData.Applied(_fixture, ids)));

        var failure = failures.Single(candidate => candidate is not null);

        await ProposalEntryFlowTestData.Proposed(_fixture, ids);
        await ProposalEntryFlowTestData.Applied(_fixture, ids);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();

        race.SaveCounts.Should().Equal(1, 1);
        failure.Should().BeAssignableTo<DbUpdateConcurrencyException>();
        metric.State.Should().Be(DefinitionDraftState.Pending);
        metric.TargetMachineId.Should().Be(ids.MachineId);
    }

    [Fact]
    public async Task Flow_AcceptedOnAMachine_MovesTheChildrenToPendingOnThatMachine()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        await ProposalEntryFlowTestData.AcceptedOnMachine(_fixture, ids);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var entry = (await _fixture.LoadProposalEntriesAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Pending);
        metric.TargetMachineId.Should().Be(ids.MachineId);
        entry.State.Should().Be(ProposalEntryState.OnMachine);
    }

    [Fact]
    public async Task Flow_AcceptedAfterAUserDismissedAChild_KeepsTheChildDismissed()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);
        await _fixture.DismissMetricDraftAsync(ids.MetricId);

        await ProposalEntryFlowTestData.AcceptedOnMachine(_fixture, ids);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Dismissed);
        status.State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task Flow_AcceptedOnAMachineWithADeletedRegistration_DismissesTheChildren()
    {
        var ids = FlowIds.Create();
        var machineId = await _fixture.RegisterTenantMachineAsync(DefinitionDraftTestData.CompanyId);
        await _fixture.DeleteMachineAsync(DefinitionDraftTestData.CompanyId, machineId);
        var idsOnDeleted = ids with { MachineId = machineId };
        await ProposalEntryFlowTestData.Proposed(_fixture, idsOnDeleted);

        await ProposalEntryFlowTestData.AcceptedOnMachine(_fixture, idsOnDeleted);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    [Fact]
    public async Task Flow_AcceptedOnAMachineWithoutARegistration_MovesTheChildrenToPending()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        await ProposalEntryFlowTestData.AcceptedOnMachine(_fixture, ids);

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task Flow_WithAProposalWithoutChildren_LeavesTheEntryOnItsOutcome()
    {
        var ids = FlowIds.Create();

        await ProposalEntryFlowTestData.AcceptedOnMachineDraft(_fixture, ids);

        var entry = (await _fixture.LoadProposalEntriesAsync()).Single();
        var metrics = await _fixture.LoadMetricDraftsAsync();

        entry.State.Should().Be(ProposalEntryState.OnMachineDraft);
        metrics.Should().BeEmpty();
    }

    private static bool IsRemoved(IDefinitionDraft draft) =>
        draft switch
        {
            MetricDefinitionDraft metric => metric.DeletedAt is not null,
            StatusDefinitionDraft status => status.DeletedAt is not null,
            OperatingPhaseDraft phase => phase.DeletedAt is not null,
            _ => false,
        };
}
