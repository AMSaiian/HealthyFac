using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class DeleteWorkOrderTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteWorkOrder_OfScheduledWorkOrder_FreesItsSlot()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        var backlog = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));
        var overlap = () => AssignAsync(tenant, backlog.Id);
        await overlap.Should().ThrowAsync<UnprocessableException>();

        await DeleteAsync(tenant, scheduled.Id, await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id));

        await AssignAsync(tenant, backlog.Id);
        var assigned = await _fixture.LoadWorkOrderAsync(backlog.Id);
        var linkRows = await _fixture.CountFileLinkRowsAsync(scheduled.Id);
        var remaining = await _fixture.CountWorkOrdersAsync(scheduled.Id);

        assigned.Current.AssigneeId.Should().Be(tenant.EmployeeId);
        remaining.Should().Be(expected: 0);
        linkRows.Should().Be(WorkOrderTestData.FileCount);
    }

    [Fact]
    public async Task DeleteWorkOrder_KeepsTheHistoryRows()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        await DeleteAsync(tenant, scheduled.Id, await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id));

        var rows = await _fixture.CountHistoryRowsAsync(scheduled.Id);

        rows.Should().Be(expected: 2);
    }

    [Fact]
    public async Task DeleteWorkOrder_WithStaleRowVersion_ThrowsConflictAndDeletesNothing()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id);
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            scheduled.Id,
            Domain.WorkOrders.WorkOrderStatus.InProgress);

        var act = () => DeleteAsync(tenant, scheduled.Id, staleRowVersion);

        await act.Should().ThrowAsync<ConflictException>();
        var remaining = await _fixture.CountWorkOrdersAsync(scheduled.Id);

        remaining.Should().Be(expected: 1);
    }

    private async Task<ShiftTenant> ArrangeTenantAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        return tenant;
    }

    private Task<CreateWorkOrderResponse> CreateScheduledAsync(ShiftTenant tenant, string time) =>
        _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));

    private async Task AssignAsync(ShiftTenant tenant, Guid workOrderId)
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<AssignWorkOrderRequest>>()
            .HandleAsync(
                new AssignWorkOrderRequest
                {
                    CompanyId = tenant.CompanyId,
                    Id = workOrderId,
                    AssigneeId = tenant.EmployeeId,
                    PlannedStartLocal = ShiftTestData.LocalStart(
                        ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime),
                    RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
                });
    }

    private async Task DeleteAsync(ShiftTenant tenant, Guid workOrderId, uint rowVersion)
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<DeleteWorkOrderRequest>>()
            .HandleAsync(
                new DeleteWorkOrderRequest { CompanyId = tenant.CompanyId, Id = workOrderId, RowVersion = rowVersion });
    }
}
