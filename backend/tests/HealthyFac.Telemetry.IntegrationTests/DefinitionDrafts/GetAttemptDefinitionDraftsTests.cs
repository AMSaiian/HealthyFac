using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class GetAttemptDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetAttemptDrafts_WithAChatDraftOfTheSameMachine_ListsOnlyTheIngestDrafts()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var ingestId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId, ProposalBuilder.ChatMessage(companyId, machineId));

        var response = await _fixture.GetAttemptDefinitionDraftsAsync(companyId, DefinitionDraftTestData.AttemptId);

        response.Metrics.Select(draft => draft.Id).Should().Equal(ingestId);
        response.Metrics.Single().Proposal.Source.Should().Be(DraftSource.Ingest);
        response.Metrics.Single().Proposal.AttemptId.Should().Be(DefinitionDraftTestData.AttemptId);
        response.Metrics.Single().Proposal.ChatId.Should().BeNull();
        response.Statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAttemptDrafts_WithDraftsInEveryState_ReturnsThemAllWithTheirTargets()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var pendingId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        var appliedId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);
        var dismissedId = await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.DismissMetricDraftAsync(dismissedId);
        var awaitingId = await _fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);

        var response = await _fixture.GetAttemptDefinitionDraftsAsync(companyId, DefinitionDraftTestData.AttemptId);

        response.Metrics.Select(draft => (draft.Id, draft.State))
            .Should()
            .BeEquivalentTo(
            [
                (pendingId, DefinitionDraftState.Pending),
                (appliedId, DefinitionDraftState.Applied),
                (dismissedId, DefinitionDraftState.Dismissed),
            ]);

        response.Metrics.Should().OnlyContain(draft => draft.TargetMachineId == machineId);
        response.Statuses.Should().ContainSingle().Which.Id.Should().Be(awaitingId);
        response.Statuses[0].State.Should().Be(DefinitionDraftState.AwaitingTarget);
    }

    [Fact]
    public async Task GetAttemptDrafts_ForAnotherCompany_ReturnsNothing()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        var response = await _fixture.GetAttemptDefinitionDraftsAsync(
            Guid.CreateVersion7(), DefinitionDraftTestData.AttemptId);

        response.Metrics.Should().BeEmpty();
        response.Statuses.Should().BeEmpty();
    }
}
