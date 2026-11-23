using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class DayScheduleTests(CoreDatabaseFixture fixture)
{
    private static readonly DateOnly SpringDstDate = new(year: 2027, month: 3, day: 28);
    private static readonly DateOnly AutumnDstDate = new(year: 2027, month: 10, day: 31);

    private static readonly DateOnly Yesterday = ShiftTestData.Today.AddDays(value: -1);

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetEmployeeDaySchedule_ForEachSource_ReturnsTemplateOverrideAndNone()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning, ShiftTestData.Afternoon));
        await _fixture.SetOverrideAsync(
            tenant, ShiftTestData.InTwoDays, ShiftTestData.Blocks(ShiftTestData.ShortMorning));

        var none = await ReadDayAsync(tenant, ShiftTestData.Today);
        var template = await ReadDayAsync(tenant, ShiftTestData.Tomorrow);
        var overrideDay = await ReadDayAsync(tenant, ShiftTestData.InTwoDays);

        none.Source.Should().Be(DayScheduleSource.None);
        none.Blocks.Should().BeEmpty();
        template.Source.Should().Be(DayScheduleSource.Template);
        template.Blocks.Should().HaveCount(expected: 2);
        overrideDay.Source.Should().Be(DayScheduleSource.Override);
        overrideDay.Blocks.Should().ContainSingle();
        template.TimeZoneId.Should().Be(ShiftTestData.ZoneId);
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_OnDstDates_ReturnsBlockInstantsOfTheCompatibleRule()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowOn(SpringDstDate.AddDays(value: -8), time: "09:30"));
        await _fixture.SetOverrideAsync(
            tenant,
            SpringDstDate,
            ShiftTestData.Blocks(ShiftTestData.NightBlock, ShiftTestData.Morning));
        _fixture.Clock.Freeze(ShiftTestData.NowOn(AutumnDstDate.AddDays(value: -8), time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, AutumnDstDate, ShiftTestData.Blocks(ShiftTestData.NightBlock));

        var spring = await ReadDayAsync(tenant, SpringDstDate);
        var autumn = await ReadDayAsync(tenant, AutumnDstDate);

        spring.Blocks.Select(block => block.EndUtc - block.StartUtc)
            .Should()
            .Equal(TimeSpan.FromHours(value: 2), TimeSpan.FromHours(value: 4));
        spring.Blocks[index: 1]
            .StartUtc.Should()
            .Be(new DateTime(year: 2027, month: 3, day: 28, hour: 5, minute: 0, second: 0, DateTimeKind.Utc));
        autumn.Blocks.Single()
            .EndUtc.Should()
            .Be(new DateTime(year: 2027, month: 10, day: 31, hour: 3, minute: 0, second: 0, DateTimeKind.Utc));
        (autumn.Blocks.Single().EndUtc - autumn.Blocks.Single().StartUtc).Should().Be(TimeSpan.FromHours(value: 4));
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_WithLateCompletedWorkOrder_ReturnsTheCompletionMomentAsOccupancyEnd()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks(ShiftTestData.Morning));
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Today,
            time: "09:00",
            minutes: 30);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "10:40"));
        await _fixture.CompleteWorkOrderAsync(workOrderId);

        var day = await ReadDayAsync(tenant, ShiftTestData.Today);

        var workOrder = day.WorkOrders.Should().ContainSingle().Which;
        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
        workOrder.OccupancyEndUtc.Should().Be(ShiftTestData.NowAt(time: "10:40").UtcDateTime);
        workOrder.EstimateMinutes.Should().Be(expected: 30);
        workOrder.IsDelayed.Should().BeFalse();
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_WithStagedWorkOrderBeforeNow_FlagsItAsDelayed()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks(ShiftTestData.Morning));
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, ShiftTestData.Today, time: "09:00", minutes: 30);
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, ShiftTestData.Today, time: "10:00", minutes: 30);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var day = await ReadDayAsync(tenant, ShiftTestData.Today);

        day.WorkOrders.Select(order => order.IsDelayed).Should().Equal(true, false);
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_WithAnInProgressWorkOrderFromYesterday_ListsItAndEndsItAtNow()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowOn(Yesterday, time: "14:00"));
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(tenant, Yesterday, time: "15:00", minutes: 120);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(Yesterday, time: "15:30"));
        await _fixture.ChangeWorkOrderStatusAsync(workOrderId, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "10:00"));

        var day = await ReadDayAsync(tenant, ShiftTestData.Today);

        var workOrder = day.WorkOrders.Should().ContainSingle().Which;
        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.OccupancyEndUtc.Should().Be(ShiftTestData.NowAt(time: "10:00").UtcDateTime);
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_WithAWorkOrderCompletedLateAfterMidnight_ListsItUntilTheCompletion()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowOn(Yesterday, time: "20:00"));
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(tenant, Yesterday, time: "22:00", minutes: 30);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "01:00"));
        await _fixture.CompleteWorkOrderAsync(workOrderId);

        var day = await ReadDayAsync(tenant, ShiftTestData.Today);

        var workOrder = day.WorkOrders.Should().ContainSingle().Which;
        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
        workOrder.OccupancyEndUtc.Should().Be(ShiftTestData.NowAt(time: "01:00").UtcDateTime);
    }

    [Fact]
    public async Task GetEmployeeDaySchedule_WithWorkOrdersThatEndedBeforeTheDate_DoesNotListThem()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowOn(Yesterday, time: "08:00"));
        var completedId = await _fixture.ArrangeStagedWorkOrderAsync(tenant, Yesterday, time: "09:00", minutes: 30);
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, Yesterday, time: "11:00", minutes: 30);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(Yesterday, time: "09:20"));
        await _fixture.CompleteWorkOrderAsync(completedId);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "10:00"));

        var day = await ReadDayAsync(tenant, ShiftTestData.Today);

        day.WorkOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCompanyDaySchedule_Always_ListsOnlyActiveEmployees()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);
        await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.DeactivatedEmail, isDeactivated: true);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var response = await _fixture.ReadAsAsync<GetCompanyDayScheduleRequest, CompanyDayScheduleResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetCompanyDayScheduleRequest { CompanyId = tenant.CompanyId, Date = ShiftTestData.Today });

        response.Employees.Should().HaveCount(expected: 2);
        response.Employees.Select(employee => employee.EmployeeId).Should().Contain(tenant.EmployeeId);
        response.TimeZoneId.Should().Be(ShiftTestData.ZoneId);
    }

    [Fact]
    public async Task GetCompanyDaySchedule_WithSeveralVersionsPerEmployee_ResolvesTheVersionInForceOfEachEmployee()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);
        var date = new DateOnly(year: 2027, month: 4, day: 12);
        await _fixture.SeedVersionAsync(tenant.EmployeeId, new DateOnly(year: 2027, month: 1, day: 1), "08:00-12:00");
        await _fixture.SeedVersionAsync(tenant.EmployeeId, new DateOnly(year: 2027, month: 3, day: 1), "09:00-11:00");
        await _fixture.SeedVersionAsync(tenant.EmployeeId, new DateOnly(year: 2027, month: 6, day: 1), "13:00-17:00");
        await _fixture.SeedVersionAsync(other.EmployeeId, new DateOnly(year: 2027, month: 2, day: 1), "10:00-12:00");
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var response = await _fixture.ReadAsAsync<GetCompanyDayScheduleRequest, CompanyDayScheduleResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetCompanyDayScheduleRequest { CompanyId = tenant.CompanyId, Date = date });

        var starts = response.Employees.ToDictionary(
            employee => employee.EmployeeId,
            employee => employee.Day.Blocks.Single().StartLocal);
        starts[tenant.EmployeeId].Should().Be(new TimeOnly(hour: 9, minute: 0));
        starts[other.EmployeeId].Should().Be(new TimeOnly(hour: 10, minute: 0));
    }

    private Task<DayScheduleResponse> ReadDayAsync(ShiftTenant tenant, DateOnly date) =>
        _fixture.ReadAsAsync<GetEmployeeDayScheduleRequest, DayScheduleResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetEmployeeDayScheduleRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                Date = date,
                Scope = AccessScope.Company,
            });
}
