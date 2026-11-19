using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreMachinesCollection.Name)]
public sealed class WorkOrderDraftMachineNameTests(CoreDatabaseFixture fixture)
{
    private const int PageLimit = 25;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Search_ForADraftOfAMachine_ReturnsTheNameOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-1");
        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);

        var page = await SearchAsync(tenant.CompanyId);

        var item = page.Items.Single(candidate => candidate.Id == draftId);
        item.MachineName.Should().Be(MachineTestData.MachineName);
        item.IsMachineDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ForADraftOfADeletedMachine_KeepsTheNameOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-2");
        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        using var scope = _fixture.CreateUserScope(tenant.CompanyId);
        var response = await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<GetWorkOrderDraftRequest, WorkOrderDraftResponse>>()
            .HandleAsync(
                new GetWorkOrderDraftRequest { CompanyId = tenant.CompanyId, Id = draftId },
                TestContext.Current.CancellationToken);

        response.MachineName.Should().Be(MachineTestData.MachineName);
        response.IsMachineDeleted.Should().BeTrue();
    }

    private async Task<GridResponse<WorkOrderDraftResponse>> SearchAsync(Guid companyId)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<SearchWorkOrderDraftsRequest, GridResponse<WorkOrderDraftResponse>>>()
            .HandleAsync(
                new SearchWorkOrderDraftsRequest
                {
                    CompanyId = companyId,
                    Offset = 0,
                    Limit = PageLimit,
                    Filter = new WorkOrderDraftGridFilter(),
                });
    }
}
