using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class ChangeWorkOrderStatusTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ChangeStatus_ToBacklog_ThrowsValidation()
    {
        var (tenant, workOrderId) = await ArrangeStagedAsync();

        var act = () => _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            workOrderId,
            WorkOrderStatus.Backlog);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task ChangeStatus_OfBacklogWorkOrder_ThrowsNotScheduled()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        var act = () => _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            created.Id,
            WorkOrderStatus.InProgress);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.NotScheduled);
    }

    [Fact]
    public async Task ChangeStatus_ToCurrentStatus_ThrowsSameStatus()
    {
        var (tenant, workOrderId) = await ArrangeStagedAsync();

        var act = () => _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            workOrderId,
            WorkOrderStatus.Staged);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SameStatus);
    }

    [Fact]
    public async Task ChangeStatus_InOwnScopeOfWorkOrderNotAssignedToTheActor_ThrowsForbidden()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Own));

        var act = () => _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            created.Id,
            WorkOrderStatus.InProgress,
            AccessScope.Own);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task ChangeStatus_WithStaleRowVersion_ThrowsConflict()
    {
        var (tenant, workOrderId) = await ArrangeStagedAsync();
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            workOrderId,
            WorkOrderStatus.InProgress);

        var act = () => _fixture.ChangeWorkOrderStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ChangeWorkOrderStatusRequest
            {
                CompanyId = tenant.CompanyId,
                Scope = AccessScope.Company,
                Id = workOrderId,
                Status = WorkOrderStatus.Blocked,
                RowVersion = staleRowVersion,
            });

        await act.Should().ThrowAsync<ConflictException>();
        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.CurrentRevision.Should().Be(expected: 3);
    }

    [Fact]
    public async Task ChangeStatus_FromStaged_CopiesAssigneeAndStartIntoTheNewRow()
    {
        var (tenant, workOrderId) = await ArrangeStagedAsync();
        var before = await _fixture.LoadWorkOrderAsync(workOrderId);

        await _fixture.ChangeStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            workOrderId,
            WorkOrderStatus.Blocked);

        var after = await _fixture.LoadWorkOrderAsync(workOrderId);

        after.CurrentRevision.Should().Be(expected: 3);
        after.History.Should().HaveCount(expected: 3);
        after.Current.Status.Should().Be(WorkOrderStatus.Blocked);
        after.Current.AssigneeId.Should().Be(before.Current.AssigneeId);
        after.Current.PlannedStartLocal.Should().Be(before.Current.PlannedStartLocal);
        after.Current.PlannedStartUtc.Should().Be(before.Current.PlannedStartUtc);
    }

    [Fact]
    public async Task ChangeStatus_WithTwoParallelChangesOfOneRowVersion_EndsWithOneSuccessAndOneConflict()
    {
        var (tenant, workOrderId) = await ArrangeStagedAsync();
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => ChangeAsync(tenant, workOrderId, WorkOrderStatus.InProgress, rowVersion)),
            race.RunAsync(() => ChangeAsync(tenant, workOrderId, WorkOrderStatus.Blocked, rowVersion)));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Should().Equal(ShiftTestData.UniqueRaceSaveCounts);
        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.CurrentRevision.Should().Be(expected: 3);
        workOrder.History.Should().HaveCount(expected: 3);
    }

    private Task ChangeAsync(ShiftTenant tenant, Guid workOrderId, WorkOrderStatus status, uint rowVersion) =>
        _fixture.ChangeWorkOrderStatusAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ChangeWorkOrderStatusRequest
            {
                CompanyId = tenant.CompanyId,
                Scope = AccessScope.Company,
                Id = workOrderId,
                Status = status,
                RowVersion = rowVersion,
            });

    private async Task<(ShiftTenant Tenant, Guid WorkOrderId)> ArrangeStagedAsync()
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

        return (tenant, created.Id);
    }
}
