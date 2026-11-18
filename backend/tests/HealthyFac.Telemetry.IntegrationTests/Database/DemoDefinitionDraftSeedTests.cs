using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Database;

[Collection(TelemetrySeedingCollection.Name)]
public sealed class DemoDefinitionDraftSeedTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedAsync_WithATestTenant_StoresEveryDemoDraftPendingWithATarget()
    {
        await _fixture.RunDemoDefinitionDraftSeedAsync();

        var drafts = await _fixture.LoadDemoDefinitionDraftsAsync();

        drafts.Metrics.Should().HaveCount(DemoDefinitionDraftSeedTestData.MetricDraftCount);
        drafts.Statuses.Should().HaveCount(DemoDefinitionDraftSeedTestData.StatusDraftCount);
        drafts.Metrics.Concat<IDefinitionDraft>(drafts.Statuses)
            .Should()
            .OnlyContain(draft => draft.State == DefinitionDraftState.Pending
                && draft.CompanyId == DevelopmentSeedTestData.CompanyId
                && (draft.TargetMachineId != null || draft.TargetMachineDraftId != null));
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_StoresOneEntryForEachMachineDraft()
    {
        await _fixture.RunDemoDefinitionDraftSeedAsync();

        var drafts = await _fixture.LoadDemoDefinitionDraftsAsync();

        var entryIds = drafts.Metrics.Concat<IDefinitionDraft>(drafts.Statuses)
            .Select(draft => draft.ProposalEntryId)
            .Distinct();

        drafts.Entries.Should().HaveCount(DemoDefinitionDraftSeedTestData.EntryCount);
        drafts.Entries.Select(entry => entry.Id).Should().BeEquivalentTo(entryIds);
        drafts.Entries.Count(entry => entry.State == ProposalEntryState.OnMachine)
            .Should()
            .Be(DemoDefinitionDraftSeedTestData.EntriesOnMachine);
        drafts.Entries.Count(entry => entry.State == ProposalEntryState.OnMachineDraft)
            .Should()
            .Be(DemoDefinitionDraftSeedTestData.EntriesOnMachineDraft);
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_AddsNothingTheSecondTime()
    {
        await _fixture.RunDemoDefinitionDraftSeedAsync();

        await _fixture.RunDemoDefinitionDraftSeedAsync();

        var drafts = await _fixture.LoadDemoDefinitionDraftsAsync();

        drafts.Metrics.Should().HaveCount(DemoDefinitionDraftSeedTestData.MetricDraftCount);
        drafts.Statuses.Should().HaveCount(DemoDefinitionDraftSeedTestData.StatusDraftCount);
        drafts.Entries.Should().HaveCount(DemoDefinitionDraftSeedTestData.EntryCount);
    }
}
