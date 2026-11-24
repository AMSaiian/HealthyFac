using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.ApplySchedule;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class ApplyScheduleChangesTests(CoreDatabaseFixture fixture)
{
    private const int TransactionsOfOneWinnerAndOneRepeatedLoser = 3;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyScheduleChanges_WithTwoMoves_MovesBothAndAddsOneRowEach()
    {
        var tenant = await ArrangeTenantAsync();
        var first = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var second = await CreateScheduledAsync(tenant, tenant.EmployeeId, WorkOrderTestData.NextTime);
        var firstRows = await _fixture.CountHistoryRowsAsync(first);

        var firstChange = await ChangeAsync(first, tenant.EmployeeId, ApplyScheduleTestData.AfternoonTime);
        var secondChange = await ChangeAsync(second, tenant.EmployeeId, ApplyScheduleTestData.SecondAfternoonTime);

        var response = await ApplyAsync(tenant, firstChange, secondChange);

        var moved = await _fixture.LoadWorkOrderAsync(first);
        var rowsAfter = await _fixture.CountHistoryRowsAsync(first);

        response.WorkOrders.Select(applied => applied.Id).Should().BeEquivalentTo([first, second]);
        moved.Current.PlannedStartLocal.Should()
            .Be(ApplyScheduleTestData.Start(ApplyScheduleTestData.AfternoonTime));
        rowsAfter.Should().Be(firstRows + 1);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithASwapBetweenTwoWorkersInOneSlot_Succeeds()
    {
        var tenant = await ArrangeTenantAsync();
        var other = await ArrangeSecondWorkerAsync(tenant);
        var first = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var second = await CreateScheduledAsync(tenant, other.EmployeeId, ApplyScheduleTestData.FirstTime);

        var firstChange = await ChangeAsync(first, other.EmployeeId, ApplyScheduleTestData.FirstTime);
        var secondChange = await ChangeAsync(second, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);

        await ApplyAsync(tenant, firstChange, secondChange);

        var swappedFirst = await _fixture.LoadWorkOrderAsync(first);
        var swappedSecond = await _fixture.LoadWorkOrderAsync(second);

        swappedFirst.Current.AssigneeId.Should().Be(other.EmployeeId);
        swappedSecond.Current.AssigneeId.Should().Be(tenant.EmployeeId);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithABacklogWorkOrderAndASlot_StagesIt()
    {
        var tenant = await ArrangeTenantAsync();
        var backlog = await CreateBacklogAsync(tenant);

        var change = await ChangeAsync(backlog, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);

        await ApplyAsync(tenant, change);

        var staged = await _fixture.LoadWorkOrderAsync(backlog);

        staged.Current.Status.Should().Be(WorkOrderStatus.Staged);
        staged.Current.AssigneeId.Should().Be(tenant.EmployeeId);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithoutAssigneeAndStart_ReturnsItToTheBacklog()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);

        var change = await ChangeAsync(scheduled, assigneeId: null, time: null);

        await ApplyAsync(tenant, change);

        var backlog = await _fixture.LoadWorkOrderAsync(scheduled);

        backlog.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        backlog.Current.AssigneeId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithAResizeOfAStartedWorkOrder_Succeeds()
    {
        var tenant = await ArrangeTenantAsync();
        var running = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        await _fixture.ChangeWorkOrderStatusAsync(running, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, ApplyScheduleTestData.LateNowTime));

        var change = await ChangeAsync(
            running, tenant.EmployeeId, ApplyScheduleTestData.FirstTime, ApplyScheduleTestData.TwoHours);

        await ApplyAsync(tenant, change);

        var resized = await _fixture.LoadWorkOrderAsync(running);

        resized.Estimate.Should().Be(TimeSpan.FromMinutes(ApplyScheduleTestData.TwoHours));
        resized.Current.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithEveryRejection_ReturnsAllFailuresAndStoresNothing()
    {
        var tenant = await ArrangeTenantAsync();
        _fixture.Clock.Release();
        var inactive = await _fixture.ArrangeOtherEmployeeAsync(
            tenant.CompanyId, ApplyScheduleTestData.InactiveWorkerEmail, isDeactivated: true);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var completed = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        await _fixture.CompleteWorkOrderAsync(completed);
        var toInactive = await CreateBacklogAsync(tenant);
        var beforeNow = await CreateBacklogAsync(tenant);
        var afterShift = await CreateBacklogAsync(tenant);
        var taken = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.AfternoonTime);
        await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.SecondAfternoonTime);
        var rowsBefore = await _fixture.CountHistoryRowsAsync(taken);

        var completedChange = await ChangeAsync(
            completed, tenant.EmployeeId, ApplyScheduleTestData.SecondAfternoonTime);
        var toInactiveChange = await ChangeAsync(toInactive, inactive.EmployeeId, ApplyScheduleTestData.FirstTime);
        var beforeNowChange = await ChangeAsync(beforeNow, tenant.EmployeeId, time: null);
        var afterShiftChange = await ChangeAsync(afterShift, tenant.EmployeeId, ApplyScheduleTestData.AfterShiftTime);
        var takenChange = await ChangeAsync(
            taken, tenant.EmployeeId, ApplyScheduleTestData.OverlappingAfternoonTime);
        var changes = new[]
        {
            completedChange,
            toInactiveChange,
            beforeNowChange with { PlannedStartLocal = ApplyScheduleTestData.PastStart },
            afterShiftChange,
            takenChange,
        };

        var act = () => ApplyAsync(tenant, changes);

        var exception = await act.Should().ThrowAsync<UnprocessableException>();
        var failures = (IReadOnlyList<ScheduleChangeFailure>)exception.Which.Extensions[ApplyScheduleTestData.Changes]!;
        var rowsAfter = await _fixture.CountHistoryRowsAsync(taken);

        failures.Select(failure => (failure.WorkOrderId, failure.Reason))
            .Should()
            .BeEquivalentTo(
            [
                (completed, ScheduleChangeRejection.NotEditable),
                (toInactive, ScheduleChangeRejection.AssigneeNotActive),
                (beforeNow, ScheduleChangeRejection.StartOutOfRange),
                (afterShift, ScheduleChangeRejection.OutsideShift),
                (taken, ScheduleChangeRejection.SlotTaken),
            ]);
        rowsAfter.Should().Be(rowsBefore);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithAStartInThePast_RejectsItAsOutOfRange()
    {
        var tenant = await ArrangeTenantAsync();
        var backlog = await CreateBacklogAsync(tenant);
        var backlogChange = await ChangeAsync(backlog, tenant.EmployeeId, time: null);
        var change = backlogChange with { PlannedStartLocal = ApplyScheduleTestData.PastStart };

        var act = () => ApplyAsync(tenant, change);

        var exception = await act.Should().ThrowAsync<UnprocessableException>();
        var failures = (IReadOnlyList<ScheduleChangeFailure>)exception.Which.Extensions[ApplyScheduleTestData.Changes]!;

        failures.Single().Reason.Should().Be(ScheduleChangeRejection.StartOutOfRange);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithAStaleRowVersion_ThrowsConflict()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var stale = ApplyScheduleTestData.Change(
            scheduled, ApplyScheduleTestData.StaleRowVersion, tenant.EmployeeId, ApplyScheduleTestData.AfternoonTime);

        var act = () => ApplyAsync(tenant, stale);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(WorkOrderErrors.Rejections.WorkOrderChanged);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithABacklogEstimateChangeNextToAMove_Succeeds()
    {
        var tenant = await ArrangeTenantAsync();
        var backlog = await CreateBacklogAsync(tenant);
        var scheduled = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);

        var backlogChange = await ChangeAsync(backlog, assigneeId: null, time: null, ApplyScheduleTestData.TwoHours);
        var moveChange = await ChangeAsync(scheduled, tenant.EmployeeId, ApplyScheduleTestData.AfternoonTime);

        await ApplyAsync(tenant, backlogChange, moveChange);

        var resized = await _fixture.LoadWorkOrderAsync(backlog);
        var moved = await _fixture.LoadWorkOrderAsync(scheduled);

        resized.Estimate.Should().Be(TimeSpan.FromMinutes(ApplyScheduleTestData.TwoHours));
        moved.Current.PlannedStartLocal.Should()
            .Be(ApplyScheduleTestData.Start(ApplyScheduleTestData.AfternoonTime));
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithAStaleRowVersionAndABrokenRule_ThrowsConflict()
    {
        var tenant = await ArrangeTenantAsync();
        var scheduled = await CreateScheduledAsync(tenant, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var stale = ApplyScheduleTestData.Change(
            scheduled, ApplyScheduleTestData.StaleRowVersion, tenant.EmployeeId, ApplyScheduleTestData.AfterShiftTime);

        var act = () => ApplyAsync(tenant, stale);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(WorkOrderErrors.Rejections.WorkOrderChanged);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithAWorkOrderOfAnotherCompany_ThrowsNotFound()
    {
        var tenant = await ArrangeTenantAsync();
        var otherCompanyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.OtherCompanyName);
        var foreign = await _fixture.CreateWorkOrderAsync(
            otherCompanyId,
            Guid.CreateVersion7(),
            WorkOrderTestData.NewRequest(otherCompanyId, AccessScope.Company));

        var act = async () => await ApplyAsync(
            tenant,
            ApplyScheduleTestData.Change(
                foreign.Id,
                await _fixture.LoadWorkOrderRowVersionAsync(foreign.Id),
                tenant.EmployeeId,
                ApplyScheduleTestData.FirstTime));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithTwoParallelAppliesToOneSlot_EndsWithOneSuccessAndOneSlotTaken()
    {
        var tenant = await ArrangeTenantAsync();
        var first = await CreateBacklogAsync(tenant);
        var second = await CreateBacklogAsync(tenant);
        var firstChange = await ChangeAsync(first, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var secondChange = await ChangeAsync(second, tenant.EmployeeId, ApplyScheduleTestData.FirstTime);
        var transactionsBefore = _fixture.Transactions.Count;
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => ApplyInScopeAsync(tenant, firstChange)),
            race.RunAsync(() => ApplyInScopeAsync(tenant, secondChange)));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<UnprocessableException>()
            .Which.Message.Should()
            .Be(WorkOrderErrors.Rejections.ScheduleChangesRejected);
        race.SaveCounts.Should().Equal(ShiftTestData.UniqueRaceSaveCounts);
        (_fixture.Transactions.Count - transactionsBefore).Should().Be(TransactionsOfOneWinnerAndOneRepeatedLoser);
    }

    private async Task<ShiftTenant> ArrangeTenantAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await SeedShiftsAsync(tenant.EmployeeId);

        return tenant;
    }

    private async Task<EmployeeSeed> ArrangeSecondWorkerAsync(ShiftTenant tenant)
    {
        _fixture.Clock.Release();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ApplyScheduleTestData.OtherWorkerEmail);
        await SeedShiftsAsync(other.EmployeeId);

        return other;
    }

    private async Task SeedShiftsAsync(Guid employeeId)
    {
        await _fixture.SeedVersionAsync(
            employeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
    }

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        return created.Id;
    }

    private async Task<Guid> CreateScheduledAsync(ShiftTenant tenant, Guid assigneeId, string time)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: assigneeId,
                plannedStartLocal: ApplyScheduleTestData.Start(time)));

        return created.Id;
    }

    private async Task<ScheduleChange> ChangeAsync(
        Guid workOrderId,
        Guid? assigneeId,
        string? time,
        int estimateMinutes = WorkOrderTestData.OneHour
    )
    {
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        return ApplyScheduleTestData.Change(workOrderId, rowVersion, assigneeId, time, estimateMinutes);
    }

    private Task<ApplyScheduleChangesResponse> ApplyAsync(ShiftTenant tenant, params ScheduleChange[] changes) =>
        _fixture.ReadAsAsync<ApplyScheduleChangesRequest, ApplyScheduleChangesResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ApplyScheduleChangesRequest { CompanyId = tenant.CompanyId, Changes = changes });

    private async Task ApplyInScopeAsync(ShiftTenant tenant, ScheduleChange change)
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ApplyScheduleChangesRequest, ApplyScheduleChangesResponse>>()
            .HandleAsync(new ApplyScheduleChangesRequest { CompanyId = tenant.CompanyId, Changes = [change] });
    }
}
