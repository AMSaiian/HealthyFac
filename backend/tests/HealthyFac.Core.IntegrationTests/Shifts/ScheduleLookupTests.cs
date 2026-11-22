using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ScheduleLookupTests(CoreDatabaseFixture fixture)
{
    private const string Afternoon = "13:00-17:00";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ScheduleDays_ForADayOffAFullDayAndAPartlyFreeDay_SummarizesEachDate()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        var saturday = ShiftTestData.Today.AddDays(value: 5);
        var tuesday = ShiftTestData.Tomorrow;
        var wednesday = ShiftTestData.InTwoDays;
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, wednesday, time: "09:00", minutes: 60);

        var response = await DaysAsync(tenant, tuesday, saturday);

        response.TimeZoneId.Should().Be(ShiftTestData.ZoneId);
        response.Days.Single(day => day.Date == saturday)
            .Should()
            .BeEquivalentTo(Summary(saturday, hasBlocks: false, longestFreeMinutes: 0));
        response.Days.Single(day => day.Date == tuesday)
            .Should()
            .BeEquivalentTo(Summary(tuesday, hasBlocks: true, longestFreeMinutes: 240));
        response.Days.Single(day => day.Date == wednesday)
            .Should()
            .BeEquivalentTo(Summary(wednesday, hasBlocks: true, longestFreeMinutes: 120));
    }

    [Fact]
    public async Task ScheduleDays_ForAPastDate_HasBlocksButNoFreeTime()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        var friday = ShiftTestData.Today.AddDays(value: -3);

        var response = await DaysAsync(tenant, friday, friday);

        response.Days.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(Summary(friday, hasBlocks: true, longestFreeMinutes: 0));
    }

    [Fact]
    public async Task ScheduleDays_ForToday_CountsFromNowOnTheGrid()
    {
        var tenant = await ArrangeAsync(time: "09:32", ShiftTestData.Morning);

        var response = await DaysAsync(tenant, ShiftTestData.Today, ShiftTestData.Today);

        response.Days.Should().ContainSingle().Which.LongestFreeMinutes.Should().Be(expected: 145);
    }

    [Fact]
    public async Task ScheduleDays_WithAnExcludedWorkOrder_CountsItsSlotAsFree()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.InTwoDays,
            time: "09:00",
            minutes: 60);

        var response = await DaysAsync(tenant, ShiftTestData.InTwoDays, ShiftTestData.InTwoDays, workOrderId);

        response.Days.Should().ContainSingle().Which.LongestFreeMinutes.Should().Be(expected: 240);
    }

    [Fact]
    public async Task FreeSpans_WithALateCompletedWorkOrder_OccupiesUpToTheCompletionMoment()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Tomorrow,
            time: "09:00",
            minutes: 60);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "10:07"));
        await _fixture.CompleteWorkOrderAsync(workOrderId);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "07:00"));

        var response = await SpansAsync(tenant, ShiftTestData.Tomorrow);

        response.Spans.Select(span => $"{span.StartLocal:HH:mm}-{span.EndLocal:HH:mm}")
            .Should()
            .Equal("08:00-09:00", "10:10-12:00");
    }

    [Fact]
    public async Task FreeSpans_ForAFreeDay_GivesOneSpanPerBlock()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning, Afternoon);

        var response = await SpansAsync(tenant, ShiftTestData.Tomorrow);

        response.Date.Should().Be(ShiftTestData.Tomorrow);
        response.Spans.Select(span => $"{span.StartLocal:HH:mm}-{span.EndLocal:HH:mm}")
            .Should()
            .Equal("08:00-12:00", "13:00-17:00");
    }

    [Fact]
    public async Task FreeSpans_WithAWorkOrder_SplitsTheBlockAndKeepsTheBoundsOnTheGrid()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, ShiftTestData.Tomorrow, time: "09:00", minutes: 60);

        var response = await SpansAsync(tenant, ShiftTestData.Tomorrow);

        response.Spans.Select(span => $"{span.StartLocal:HH:mm}-{span.EndLocal:HH:mm}")
            .Should()
            .Equal("08:00-09:00", "10:00-12:00");
        response.Spans.Should().OnlyContain(span => span.StartLocal.Minute % 5 == 0 && span.EndLocal.Minute % 5 == 0);
    }

    [Fact]
    public async Task FreeSpans_ForToday_StartAtNowOnTheGrid()
    {
        var tenant = await ArrangeAsync(time: "09:32", ShiftTestData.Morning);

        var response = await SpansAsync(tenant, ShiftTestData.Today);

        response.Spans.Should()
            .ContainSingle()
            .Which.StartLocal.ToString(format: "HH:mm")
            .Should()
            .Be(expected: "09:35");
    }

    [Fact]
    public async Task FreeSpans_WithAnExcludedWorkOrder_CountsItsSlotAsFree()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Tomorrow,
            time: "09:00",
            minutes: 60);

        var response = await SpansAsync(tenant, ShiftTestData.Tomorrow, workOrderId);

        response.Spans.Should()
            .ContainSingle()
            .Which.StartLocal.ToString(format: "HH:mm")
            .Should()
            .Be(expected: "08:00");
    }

    [Fact]
    public async Task Lookups_ForAnEmployeeOfAnotherCompany_ThrowNotFound()
    {
        var tenant = await ArrangeAsync(time: "09:30", ShiftTestData.Morning);
        _fixture.Clock.Release();
        var otherCompanyId = await _fixture.CreateCompanyAsync(ShiftTestData.OtherCompanyName);
        var stranger = await _fixture.ArrangeOtherEmployeeAsync(otherCompanyId, ShiftTestData.OtherEmail);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var strangerTenant = tenant with { EmployeeId = stranger.EmployeeId };

        var days = () => DaysAsync(strangerTenant, ShiftTestData.Tomorrow, ShiftTestData.Tomorrow);
        var spans = () => SpansAsync(strangerTenant, ShiftTestData.Tomorrow);

        await days.Should().ThrowAsync<NotFoundException>();
        await spans.Should().ThrowAsync<NotFoundException>();
    }

    private static ScheduleDayResponse Summary(DateOnly date, bool hasBlocks, int longestFreeMinutes) =>
        new() { Date = date, HasBlocks = hasBlocks, LongestFreeMinutes = longestFreeMinutes };

    private async Task<ShiftTenant> ArrangeAsync(string time, params string[] ranges)
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(tenant.EmployeeId, ShiftTestData.Today.AddDays(value: -30), ranges);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time));

        return tenant;
    }

    private async Task<ScheduleDaysResponse> DaysAsync(
        ShiftTenant tenant,
        DateOnly from,
        DateOnly to,
        Guid? excludeWorkOrderId = null
    ) =>
        await _fixture.ReadAsAsync<GetEmployeeScheduleDaysRequest, ScheduleDaysResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetEmployeeScheduleDaysRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                From = from,
                To = to,
                ExcludeWorkOrderId = excludeWorkOrderId,
                Scope = AccessScope.Company,
            });

    private async Task<FreeSpansResponse> SpansAsync(
        ShiftTenant tenant,
        DateOnly date,
        Guid? excludeWorkOrderId = null
    ) =>
        await _fixture.ReadAsAsync<GetEmployeeFreeSpansRequest, FreeSpansResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetEmployeeFreeSpansRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                Date = date,
                ExcludeWorkOrderId = excludeWorkOrderId,
                Scope = AccessScope.Company,
            });
}
