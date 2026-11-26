using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class PhaseDraftTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Store_WithAPhaseEntry_StoresThePhaseWithItsRowsAwaitingTargetInTheSameSave()
    {
        var ids = FlowIds.Create();

        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();
        phase.Id.Should().Be(ids.PhaseId);
        phase.State.Should().Be(DefinitionDraftState.AwaitingTarget);
        phase.ProposalEntryId.Should().Be(ids.EntryId);
        phase.Code.Should().Be(PhaseDraftTestData.PhaseCode);
        phase.Conditions.Should().ContainSingle().Which.StatusDraftId.Should().Be(ids.StatusId);
        phase.Limits.Should().ContainSingle().Which.MetricDraftId.Should().Be(ids.MetricId);
        metrics.Should().ContainSingle();
        statuses.Should().ContainSingle();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Store_Twice_StoresNoSecondPhaseDraft()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        var phases = await _fixture.LoadPhaseDraftsAsync();
        phases.Should().ContainSingle();
        phases.Single().Conditions.Should().ContainSingle();
    }

    [Fact]
    public async Task Flow_AcceptedOnAMachine_MovesThePhaseDraftToPendingOnThatMachine()
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        await ProposalEntryFlowTestData.AcceptedOnMachine(_fixture, ids);

        var phase = (await _fixture.LoadPhaseDraftsAsync()).Single();
        phase.State.Should().Be(DefinitionDraftState.Pending);
        phase.TargetMachineId.Should().Be(ids.MachineId);
    }
}
