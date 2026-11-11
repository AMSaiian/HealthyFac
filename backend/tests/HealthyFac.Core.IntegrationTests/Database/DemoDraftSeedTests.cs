using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreSeedingCollection.Name)]
public sealed class DemoDraftSeedTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedAsync_WithATestTenant_CreatesThePendingDraftsAsTheSupervisor()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var drafts = await _fixture.LoadDemoDraftsAsync();

        drafts.Machines.Should().HaveCount(DemoDraftSeedTestData.MachineDraftCount);
        drafts.Metrics.Should().HaveCount(DemoDraftSeedTestData.MetricDraftCount);
        drafts.Statuses.Should().HaveCount(DemoDraftSeedTestData.StatusDraftCount);
        drafts.Machines.Should().OnlyContain(draft => draft.State == DraftState.Pending);
        drafts.Metrics.Should().OnlyContain(draft => draft.State == DraftState.Pending);
        drafts.Statuses.Should().OnlyContain(draft => draft.State == DraftState.Pending);
        drafts.Machines.Should().OnlyContain(draft => draft.CompanyId == DatabaseTestData.TestTenantCompanyId);
        drafts.Machines.Should().OnlyContain(draft => draft.CreatedBy == DatabaseTestData.TestSupervisorIdentityId);
        drafts.Metrics.Should().OnlyContain(draft => draft.CreatedBy == DatabaseTestData.TestSupervisorIdentityId);
        drafts.Statuses.Should().OnlyContain(draft => draft.CreatedBy == DatabaseTestData.TestSupervisorIdentityId);
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_TargetsTheSeedMachineAndTheCreateDrafts()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var drafts = await _fixture.LoadDemoDraftsAsync();

        var cncDraft = drafts.Machines.Single(draft => draft.TargetMachineId != null);
        var createIds = drafts.Machines.Where(draft => draft.IsCreate).Select(draft => draft.Id).ToList();
        cncDraft.TargetMachineId.Should().Be(DatabaseTestData.FirstSeedMachineId);
        cncDraft.Name.Should().Be(DemoDraftSeedTestData.CncMachineName);
        cncDraft.SerialNumber.Should().Be(DemoDraftSeedTestData.CncSerialNumber);
        createIds.Should().HaveCount(DemoDraftSeedTestData.MachineDraftCount - 1);
        drafts.Metrics.Where(draft => draft.TargetMachineDraftId != null)
            .Should().OnlyContain(draft => createIds.Contains(draft.TargetMachineDraftId!.Value));
        drafts.Statuses.Where(draft => draft.TargetMachineDraftId != null)
            .Should().OnlyContain(draft => createIds.Contains(draft.TargetMachineDraftId!.Value));
        drafts.Metrics.Where(draft => draft.TargetMachineId != null)
            .Should().OnlyContain(draft => draft.TargetMachineId == DatabaseTestData.FirstSeedMachineId);
    }

    [Fact]
    public async Task SeedAsync_AfterTheTenantSeed_AddsNoOutboxRow()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        var before = await _fixture.CountAllOutboxMessagesAsync();

        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var after = await _fixture.CountAllOutboxMessagesAsync();

        after.Should().Be(before);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_InTheDevelopmentEnvironment_CreatesTheDemoDrafts()
    {
        await _fixture.RunInitializerSeedAsync(
            DatabaseTestData.ForcedRecreationWithSeedMachinesOptions,
            Environments.Development);

        var drafts = await _fixture.LoadDemoDraftsAsync();

        drafts.Machines.Should().HaveCount(DemoDraftSeedTestData.MachineDraftCount);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OutsideTheDevelopmentEnvironment_SeedsTheTenantButNoDemoDraft()
    {
        await _fixture.RunInitializerSeedAsync(
            DatabaseTestData.ForcedRecreationWithSeedMachinesOptions,
            Environments.Production);

        var drafts = await _fixture.LoadDemoDraftsAsync();
        var companyCount = await _fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);

        companyCount.Should().Be(1);
        drafts.Machines.Should().BeEmpty();
        drafts.Metrics.Should().BeEmpty();
        drafts.Statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_RunTwice_AddsNothing()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var drafts = await _fixture.LoadDemoDraftsAsync();

        drafts.Machines.Should().HaveCount(DemoDraftSeedTestData.MachineDraftCount);
        drafts.Metrics.Should().HaveCount(DemoDraftSeedTestData.MetricDraftCount);
        drafts.Statuses.Should().HaveCount(DemoDraftSeedTestData.StatusDraftCount);
    }

    [Fact]
    public async Task SeedAsync_AfterAForcedRecreation_SeedsTheDraftsAgain()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.DismissAllMachineDraftsAsync();

        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);
        await _fixture.RunDemoDraftSeedAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var drafts = await _fixture.LoadDemoDraftsAsync();

        drafts.Machines.Should().HaveCount(DemoDraftSeedTestData.MachineDraftCount);
        drafts.Machines.Should().OnlyContain(draft => draft.State == DraftState.Pending);
    }

    [Fact]
    public async Task SeedAsync_WithoutATestTenant_CreatesNothing()
    {
        await _fixture.RunDemoDraftSeedAsync(new HealthyFac.Core.Infrastructure.Database.Seeding.SeedingOptions());

        var drafts = await _fixture.LoadDemoDraftsAsync();

        drafts.Machines.Should().BeEmpty();
        drafts.Metrics.Should().BeEmpty();
        drafts.Statuses.Should().BeEmpty();
    }
}
