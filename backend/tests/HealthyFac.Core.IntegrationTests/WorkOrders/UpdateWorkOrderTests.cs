using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
public sealed class UpdateWorkOrderTests(CoreDatabaseFixture fixture)
{
    private static readonly DateTimeOffset LateCompletionAt =
        ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "10:40");

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateWorkOrder_WithAddedAndRemovedFiles_ReplacesTheLinks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA, WorkOrderTestData.FileB);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileB, WorkOrderTestData.FileC]));

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);
        var linkRows = await _fixture.CountFileLinkRowsAsync(workOrderId);

        workOrder.Files.Select(link => link.FileId)
            .Should()
            .BeEquivalentTo([WorkOrderTestData.FileB, WorkOrderTestData.FileC]);

        workOrder.Title.Should().Be(WorkOrderTestData.ChangedTitle);
        workOrder.Priority.Should().Be(WorkOrderPriority.Critical);
        linkRows.Should().Be(expected: 3);
    }

    [Fact]
    public async Task Update_DroppingLink_SoftDeletesThatLink()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA, WorkOrderTestData.FileB);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileB]));

        var links = await _fixture.LoadFileLinkRowsAsync(workOrderId);

        links.Should().HaveCount(expected: 2);
        links.Single(link => link.FileId == WorkOrderTestData.FileA).DeletedAt.Should().NotBeNull();
        links.Single(link => link.FileId == WorkOrderTestData.FileB).DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWorkOrder_WithUnchangedFiles_AddsNoLinkRow()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA, WorkOrderTestData.FileB);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileB, WorkOrderTestData.FileA]));

        var linkRows = await _fixture.CountFileLinkRowsAsync(workOrderId);
        var newRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        linkRows.Should().Be(expected: 2);
        newRowVersion.Should().NotBe(rowVersion);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithStaleRowVersion_ThrowsConflictAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                staleRowVersion,
                [WorkOrderTestData.FileA]));

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                staleRowVersion,
                [WorkOrderTestData.FileB],
                title: WorkOrderTestData.Title));

        await act.Should().ThrowAsync<ConflictException>();
        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.Title.Should().Be(WorkOrderTestData.ChangedTitle);
        workOrder.Files.Select(link => link.FileId).Should().Equal(WorkOrderTestData.FileA);
    }

    [Fact]
    public async Task UpdateWorkOrder_InOwnScopeWithNewEstimate_ThrowsForbidden()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Own,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileA],
                WorkOrderTestData.LongerEstimate));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateWorkOrder_WithLongerEstimateThatOverlapsTheNextOne_ThrowsSlotTaken()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();

        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);

        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var first = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        await CreateScheduledAsync(tenant, WorkOrderTestData.NextTime);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(first.Id);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                first.Id,
                rowVersion,
                [],
                WorkOrderTestData.LongerEstimate,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
        var workOrder = await _fixture.LoadWorkOrderAsync(first.Id);

        workOrder.Estimate.Should().Be(TimeSpan.FromMinutes(WorkOrderTestData.OneHour));
    }

    [Fact]
    public async Task UpdateWorkOrder_WithOnlyANewEstimateOfAScheduledOrder_AddsOneRowWithTheNewEstimate()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                scheduled.Id,
                rowVersion,
                [],
                WorkOrderTestData.LongerEstimate,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);
        var previous = workOrder.History.Single(row => row.Revision == workOrder.CurrentRevision - 1);

        workOrder.CurrentRevision.Should().Be(expected: 3);
        workOrder.Current.Estimate.Should().Be(TimeSpan.FromMinutes(WorkOrderTestData.LongerEstimate));
        workOrder.Current.Status.Should().Be(previous.Status);
        workOrder.Current.AssigneeId.Should().Be(previous.AssigneeId);
        workOrder.Current.PlannedStartUtc.Should().Be(previous.PlannedStartUtc);
        previous.Estimate.Should().Be(TimeSpan.FromMinutes(WorkOrderTestData.OneHour));
    }

    [Fact]
    public async Task UpdateWorkOrder_WithTheSameEstimate_AddsNoRow()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                scheduled.Id,
                rowVersion,
                [],
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);

        workOrder.CurrentRevision.Should().Be(expected: 2);
        workOrder.History.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAssigneeAndStartOfABacklogOrder_AssignsItInOneRow()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);

        await UpdateScheduleAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.CurrentRevision.Should().Be(expected: 2);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
        workOrder.Current.AssigneeId.Should().Be(tenant.EmployeeId);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAnotherStartOfAScheduledOrder_MovesItAndKeepsTheStatus()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        await _fixture.ChangeWorkOrderStatusAsync(scheduled.Id, WorkOrderStatus.InProgress);

        await UpdateScheduleAsync(tenant, scheduled.Id, tenant.EmployeeId, WorkOrderTestData.NextTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);

        workOrder.CurrentRevision.Should().Be(expected: 4);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.InProgress);

        workOrder.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime));
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAnotherAssigneeOfAScheduledOrder_MovesItAndKeepsTheStatus()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        await _fixture.ChangeWorkOrderStatusAsync(scheduled.Id, WorkOrderStatus.InProgress);
        _fixture.Clock.Release();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, WorkOrderTestData.OtherAssigneeEmail);

        await _fixture.SeedVersionAsync(
            other.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);

        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        _fixture.RealtimeQueue.Clear();

        await UpdateScheduleAsync(tenant, scheduled.Id, other.EmployeeId, WorkOrderTestData.ScheduledTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);
        var pushes = _fixture.RealtimeQueue.Items.Select(item => item.Push).ToList();

        workOrder.CurrentRevision.Should().Be(expected: 4);
        workOrder.Current.AssigneeId.Should().Be(other.EmployeeId);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.InProgress);

        pushes.Single(push => push.UserId == other.IdentityUserId).Notification!.Kind.Should()
            .Be(RealtimeNotificationKind.WorkOrderAssigned);

        pushes.Should().NotContain(push => push.UserId == tenant.IdentityUserId);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAnotherAssigneeWhoIsBusyAtTheStart_ThrowsSlotTaken()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        _fixture.Clock.Release();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, WorkOrderTestData.OtherAssigneeEmail);

        await _fixture.SeedVersionAsync(
            other.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);

        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: other.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        var act = () => UpdateScheduleAsync(
            tenant, scheduled.Id, other.EmployeeId, WorkOrderTestData.ScheduledTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithoutAssigneeAndStartOfAScheduledOrder_ReturnsItToTheBacklog()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        await UpdateScheduleAsync(tenant, scheduled.Id, assigneeId: null, time: null);

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);

        workOrder.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        workOrder.Current.AssigneeId.Should().BeNull();
        workOrder.Current.PlannedStartLocal.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAStartThatOverlapsAnotherOrder_ThrowsSlotTaken()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var first = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        await CreateScheduledAsync(tenant, WorkOrderTestData.NextTime);

        var act = () => UpdateScheduleAsync(tenant, first.Id, tenant.EmployeeId, time: "09:30");

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAStartOutsideEveryBlock_ThrowsOutsideShift()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        var act = () => UpdateScheduleAsync(tenant, scheduled.Id, tenant.EmployeeId, time: "20:00");

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.OutsideShift);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAnInactiveAssignee_ThrowsAssigneeNotActive()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        _fixture.Clock.Release();

        var gone = await _fixture.ArrangeOtherEmployeeAsync(
            tenant.CompanyId,
            WorkOrderTestData.DeactivatedEmail,
            isDeactivated: true);

        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        var act = () => UpdateScheduleAsync(tenant, workOrderId, gone.EmployeeId, WorkOrderTestData.ScheduledTime);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.AssigneeNotActive);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithAnAssigneeWithoutAStart_ThrowsValidation()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [],
                assigneeId: tenant.EmployeeId));

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task UpdateWorkOrder_WithANewStartAndANewEstimate_AddsOneRowWithBoth()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        await UpdateScheduleAsync(
            tenant,
            scheduled.Id,
            tenant.EmployeeId,
            WorkOrderTestData.NextTime,
            WorkOrderTestData.LongerEstimate);

        var workOrder = await _fixture.LoadWorkOrderAsync(scheduled.Id);

        workOrder.CurrentRevision.Should().Be(expected: 3);
        workOrder.Current.Estimate.Should().Be(TimeSpan.FromMinutes(WorkOrderTestData.LongerEstimate));

        workOrder.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime));
    }

    [Fact]
    public async Task UpdateWorkOrder_InOwnScopeWithAnotherStart_ThrowsForbidden()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(scheduled.Id);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Own,
                scheduled.Id,
                rowVersion,
                [],
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime)));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task UpdateScheduleAsync(
        ShiftTenant tenant,
        Guid workOrderId,
        Guid? assigneeId,
        string? time,
        int estimateMinutes = WorkOrderTestData.OneHour
    ) =>
        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
                [],
                estimateMinutes,
                assigneeId: assigneeId,
                plannedStartLocal: time is null ? null : ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));

    [Fact]
    public async Task UpdateWorkOrder_OfACompletedWorkOrderWithAnotherEstimate_Returns422AndKeepsTheOccupancyEnd()
    {
        var (tenant, workOrderId) = await ArrangeLateCompletedAsync();
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        var rowsBefore = await _fixture.CountHistoryRowsAsync(workOrderId);
        _fixture.Clock.Freeze(LateCompletionAt.AddDays(days: 1));

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [],
                WorkOrderTestData.LongerEstimate,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "09:00")));

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.CompletedScheduleLocked);

        var rowsAfter = await _fixture.CountHistoryRowsAsync(workOrderId);
        var day = await ReadDayAsync(tenant);

        rowsAfter.Should().Be(rowsBefore);
        day.WorkOrders.Should().ContainSingle().Which.OccupancyEndUtc.Should().Be(LateCompletionAt.UtcDateTime);
    }

    [Fact]
    public async Task UpdateWorkOrder_OfACompletedWorkOrderWithAnotherStart_Returns422()
    {
        var (tenant, workOrderId) = await ArrangeLateCompletedAsync();
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [],
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "13:00")));

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.CompletedScheduleLocked);
    }

    [Fact]
    public async Task UpdateWorkOrder_OfACompletedWorkOrderWithOnlyAnotherTitle_SavesWithoutAHistoryRow()
    {
        var (tenant, workOrderId) = await ArrangeLateCompletedAsync();
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        var rowsBefore = await _fixture.CountHistoryRowsAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [],
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "09:00")));

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);
        var rowsAfter = await _fixture.CountHistoryRowsAsync(workOrderId);

        workOrder.Title.Should().Be(WorkOrderTestData.ChangedTitle);
        rowsAfter.Should().Be(rowsBefore);
    }

    private async Task<(ShiftTenant Tenant, Guid WorkOrderId)> ArrangeLateCompletedAsync()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var created = await CreateScheduledAsync(tenant, time: "09:00");

        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, created.Id, WorkOrderStatus.InProgress);

        _fixture.Clock.Freeze(LateCompletionAt);

        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, created.Id, WorkOrderStatus.Completed);

        return (tenant, created.Id);
    }

    private Task<DayScheduleResponse> ReadDayAsync(ShiftTenant tenant) =>
        _fixture.ReadAsAsync<GetEmployeeDayScheduleRequest, DayScheduleResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetEmployeeDayScheduleRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                Date = ShiftTestData.Tomorrow,
                Scope = AccessScope.Company,
            });

    private async Task<ShiftTenant> ArrangeScheduledTenantAsync()
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

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant, params Guid[] fileIds)
    {
        var response = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.WithFiles(
                WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company),
                fileIds));

        return response.Id;
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
}
