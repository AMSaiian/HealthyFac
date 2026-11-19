using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreMachinesCollection.Name)]
public sealed class WorkOrderDraftConstraintTests(CoreDatabaseFixture fixture)
{
    private const string ValidState = "'dismissed'";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(WorkOrderDraftTestData.InvalidValues), MemberType = typeof(WorkOrderDraftTestData))]
    public async Task Update_WithAValueThatBreaksACheck_Fails(string caseName, string column, string sqlValue)
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.SetWorkOrderDraftColumnAsync(draftId, column, sqlValue);

        await act.Should().ThrowAsync<Exception>(caseName);
    }

    [Fact]
    public async Task Update_WithAKnownState_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.SetWorkOrderDraftColumnAsync(draftId, column: "state", ValidState);

        await act.Should().NotThrowAsync();
    }
}
