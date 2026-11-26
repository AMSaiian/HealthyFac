using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class GetPendingDefinitionDraftCountsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetPendingCounts_WithPendingAndAwaitingDrafts_CountsOnlyThePendingOnes()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var emptyMachineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StorePendingStatusDraftOnMachineAsync(companyId, machineId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);
        await _fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);

        var response = await _fixture.GetPendingDefinitionDraftCountsAsync(companyId);

        response.Counts.Should().ContainSingle().Which.MachineId.Should().Be(machineId);
        response.Counts[0].Count.Should().Be(expected: 2);
        response.Counts.Select(count => count.MachineId).Should().NotContain(emptyMachineId);
    }

    [Fact]
    public async Task GetPendingCounts_WithAMetricDraftAndAPhaseDraft_CountsThePhaseDraftToo()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.StorePendingPhaseProposalAsync(tenant, machineId);

        var response = await _fixture.GetPendingDefinitionDraftCountsAsync(tenant.CompanyId);

        response.Counts.Should().ContainSingle().Which.Count.Should().Be(expected: 3);
    }

    [Fact]
    public async Task GetPendingCounts_ForADeletedRegistration_LeavesTheMachineOut()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.DeleteMachineAsync(companyId, machineId);

        var response = await _fixture.GetPendingDefinitionDraftCountsAsync(companyId);

        response.Counts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingCounts_ForAnotherCompany_ReturnsOnlyItsOwnMachines()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        var response = await _fixture.GetPendingDefinitionDraftCountsAsync(Guid.CreateVersion7());

        response.Counts.Should().BeEmpty();
    }
}
