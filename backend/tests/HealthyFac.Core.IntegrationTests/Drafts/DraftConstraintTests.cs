using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class DraftConstraintTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DraftTestData.ChildTables), MemberType = typeof(DraftTestData))]
    public async Task InsertChildDraft_WithBothTargets_FailsOnTheOneTargetCheck(string table, string checkName)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var machineDraftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);

        var act = () => _fixture.InsertChildDraftRowAsync(table, tenant.CompanyId, machineId, machineDraftId);

        var thrown = await act.Should().ThrowAsync<Exception>();

        thrown.Which.ToString().Should().Contain(checkName);
    }

    [Theory]
    [MemberData(nameof(DraftTestData.ChildTables), MemberType = typeof(DraftTestData))]
    public async Task InsertChildDraft_WithNoTarget_FailsOnTheOneTargetCheck(string table, string checkName)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.InsertChildDraftRowAsync(
            table, tenant.CompanyId, targetMachineId: null, targetMachineDraftId: null);

        var thrown = await act.Should().ThrowAsync<Exception>();

        thrown.Which.ToString().Should().Contain(checkName);
    }

    [Theory]
    [MemberData(nameof(DraftTestData.ChildTables), MemberType = typeof(DraftTestData))]
    public async Task InsertChildDraft_WithMachineTarget_Succeeds(string table, string checkName)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);

        var act = () => _fixture.InsertChildDraftRowAsync(
            table, tenant.CompanyId, machineId, targetMachineDraftId: null);

        await act.Should().NotThrowAsync();
        checkName.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [MemberData(nameof(DraftTestData.ChildTables), MemberType = typeof(DraftTestData))]
    public async Task InsertChildDraft_WithMachineDraftTarget_Succeeds(string table, string checkName)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineDraftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);

        var act = () => _fixture.InsertChildDraftRowAsync(
            table, tenant.CompanyId, targetMachineId: null, machineDraftId);

        await act.Should().NotThrowAsync();
        checkName.Should().NotBeNullOrEmpty();
    }
}
