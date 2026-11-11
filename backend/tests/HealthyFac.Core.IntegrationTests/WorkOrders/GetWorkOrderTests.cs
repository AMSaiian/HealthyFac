using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class GetWorkOrderTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetWorkOrder_InOwnScope_ReturnsAssignedAndOwnBacklogOnly()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var supervisorId = Guid.CreateVersion7();
        var assigned = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            supervisorId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));
        var ownBacklog = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Own));
        var foreignBacklog = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            supervisorId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        var assignedResponse = await GetAsync(tenant, assigned.Id, AccessScope.Own);
        var ownBacklogResponse = await GetAsync(tenant, ownBacklog.Id, AccessScope.Own);
        var foreign = () => GetAsync(tenant, foreignBacklog.Id, AccessScope.Own);
        var foreignAsSupervisor = await GetAsync(tenant, foreignBacklog.Id, AccessScope.Company);

        assignedResponse.Status.Should().Be(WorkOrderStatus.Staged);
        assignedResponse.AssigneeId.Should().Be(tenant.EmployeeId);
        assignedResponse.AssigneeName.Should().Contain(ShiftTestData.EmployeeFirstName);
        assignedResponse.FileIds.Should().HaveCount(WorkOrderTestData.FileCount);
        ownBacklogResponse.Status.Should().Be(WorkOrderStatus.Backlog);
        ownBacklogResponse.AssigneeId.Should().BeNull();
        await foreign.Should().ThrowAsync<NotFoundException>();
        foreignAsSupervisor.Id.Should().Be(foreignBacklog.Id);
    }

    [Fact]
    public async Task GetWorkOrder_OfOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var otherCompanyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.OtherCompanyName);
        var created = await _fixture.CreateWorkOrderAsync(
            otherCompanyId,
            Guid.CreateVersion7(),
            WorkOrderTestData.NewRequest(otherCompanyId, AccessScope.Company));

        var act = () => GetAsync(tenant, created.Id, AccessScope.Company);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetWorkOrderHistory_Always_ReturnsRowsNewestFirst()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        var history = await _fixture.ReadAsAsync<GetWorkOrderHistoryRequest, IReadOnlyList<WorkOrderHistoryResponse>>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetWorkOrderHistoryRequest
            {
                CompanyId = tenant.CompanyId,
                Id = created.Id,
                Scope = AccessScope.Company,
            });

        history.Select(row => row.Revision).Should().Equal(2, 1);
        history[index: 0].Status.Should().Be(WorkOrderStatus.Staged);
        history[index: 0].AssigneeName.Should().Contain(ShiftTestData.EmployeeFirstName);
        history[index: 1].Status.Should().Be(WorkOrderStatus.Backlog);
        history[index: 1].CreatedBy.Should().Be(tenant.IdentityUserId);
    }

    [Fact]
    public async Task GetWorkOrder_WithDeletedMachine_ShowsTheNameAndTheFlag()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            WorkOrderTestData.MachineName,
            WorkOrderTestData.MachineSerialNumber);
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company, machineId));
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var response = await GetAsync(tenant, created.Id, AccessScope.Company);

        response.MachineId.Should().Be(machineId);
        response.MachineName.Should().Be(WorkOrderTestData.MachineName);
        response.IsMachineDeleted.Should().BeTrue();
    }

    private Task<WorkOrderResponse> GetAsync(ShiftTenant tenant, Guid workOrderId, AccessScope scope) =>
        _fixture.ReadAsAsync<GetWorkOrderRequest, WorkOrderResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetWorkOrderRequest { CompanyId = tenant.CompanyId, Id = workOrderId, Scope = scope });
}
