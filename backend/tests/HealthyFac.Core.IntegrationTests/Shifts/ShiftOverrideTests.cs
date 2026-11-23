using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Application.Shifts.ChangeChecks;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ShiftOverrideTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ShiftTestData.DatesOutsideTheRange), MemberType = typeof(ShiftTestData))]
    public async Task SetOverride_WithDateOutsideTheRange_ThrowsValidation(string caseName, int dayOffset)
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var act = () => _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today.AddDays(dayOffset),
            ShiftTestData.Blocks(ShiftTestData.Morning));

        await act.Should().ThrowAsync<AppValidationException>(caseName);
    }

    [Fact]
    public async Task SetOverride_WithEmptyBlocks_IsADayOff()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));

        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, []);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        day.Source.Should().Be(DayScheduleSource.Override);
        day.Blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteOverride_Always_ReturnsTheDateToTheTemplate()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, []);

        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        day.Source.Should().Be(DayScheduleSource.Template);
        day.Blocks.Should().ContainSingle();
    }

    [Fact]
    public async Task SetOverride_ReplacingBlocksOfExistingDate_ShowsOnlyTheNewBlocks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.ShortMorning),
            rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(ShiftTestData.ParseTime(time: "09:00"));
    }

    [Fact]
    public async Task SetOverride_ReplacingBlocksOfExistingDate_SoftDeletesTheOldBlocks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.ShortMorning),
            rowVersion);

        var blocks = await _fixture.LoadOverrideBlockRowsAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        blocks.Should().HaveCount(expected: 3);
        blocks.Count(block => block.DeletedAt != null).Should().Be(expected: 2);
    }

    [Fact]
    public async Task DeleteOverride_Always_SoftDeletesItsBlocks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, rowVersion);

        var blocks = await _fixture.LoadOverrideBlockRowsAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);

        blocks.Should().HaveCount(expected: 2).And.OnlyContain(block => block.DeletedAt != null);
    }

    [Fact]
    public async Task SetOverride_ThatOrphansAScheduledWorkOrder_ThrowsWithTheWorkOrder()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Tomorrow,
            time: "14:00",
            minutes: 60);

        var act = () => _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, []);

        var exception = (await act.Should().ThrowAsync<UnprocessableException>()).Which;
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        exception.Extensions[ShiftChangeGuard.WorkOrdersKey]
            .Should()
            .BeAssignableTo<IReadOnlyList<AffectedWorkOrder>>()
            .Which.Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be(workOrderId);
        day.Source.Should().Be(DayScheduleSource.Template);
    }

    [Fact]
    public async Task SetOverride_ForTodayThatRemovesAnEndedBlock_ThrowsPastBlockChanged()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "14:00"));

        var act = () => _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today,
            ShiftTestData.Blocks(ShiftTestData.Afternoon));

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(ShiftErrors.Rejections.PastBlockChanged);
    }

    [Fact]
    public async Task SetOverride_WithTwoParallelReplacesOfOneRowVersion_EndsWithOneSuccessAndOneConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.SetOverrideAsync(
                tenant,
                ShiftTestData.Tomorrow,
                ShiftTestData.Blocks(ShiftTestData.Afternoon),
                rowVersion)),
            race.RunAsync(() => _fixture.SetOverrideAsync(
                tenant,
                ShiftTestData.Tomorrow,
                ShiftTestData.Blocks(ShiftTestData.ShortMorning),
                rowVersion)));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Order().Should().Equal(ShiftTestData.RaceSaveCounts);
    }

    [Fact]
    public async Task SetOverride_WithTwoParallelCreatesOfOneDate_EndsWithOneSuccessAndOneConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.SetOverrideAsync(
                tenant,
                ShiftTestData.Tomorrow,
                ShiftTestData.Blocks(ShiftTestData.Afternoon))),
            race.RunAsync(() => _fixture.SetOverrideAsync(
                tenant,
                ShiftTestData.Tomorrow,
                ShiftTestData.Blocks(ShiftTestData.ShortMorning))));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Should().Equal(ShiftTestData.UniqueRaceSaveCounts);
    }

    [Fact]
    public async Task SetOverride_WithoutARowVersionOnADateWithAnOverride_ThrowsOverrideExistsAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Afternoon));

        var act = () => _fixture.SetOverrideAsync(
            tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.ShortMorning));

        await act.Should().ThrowAsync<ConflictException>().WithMessage(ShiftErrors.Rejections.OverrideExists);
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal(ShiftTestData.Afternoon);
    }

    [Fact]
    public async Task SetOverride_WithARowVersionOnADateWithoutAnOverride_ThrowsOverrideChanged()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var act = () => _fixture.SetOverrideAsync(
            tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Afternoon), rowVersion: 1);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(ShiftErrors.Rejections.OverrideChanged);
    }

    [Fact]
    public async Task DeleteOverride_WithStaleRowVersion_ThrowsConflictAndKeepsTheOverride()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));
        var staleRowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);
        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.Blocks(ShiftTestData.Afternoon),
            staleRowVersion);

        var act = () => _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, staleRowVersion);

        var conflict = await act.Should().ThrowAsync<ConflictException>();
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        conflict.WithMessage(ShiftErrors.Rejections.OverrideChanged);
        day.Source.Should().Be(DayScheduleSource.Override);
    }

    [Fact]
    public async Task DeleteOverride_RacingAReplaceOfOneRowVersion_EndsWithOneSuccessAndOneConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Tomorrow);
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.SetOverrideAsync(
                tenant,
                ShiftTestData.Tomorrow,
                ShiftTestData.Blocks(ShiftTestData.Afternoon),
                rowVersion)),
            race.RunAsync(() => _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, rowVersion)));

        var deleteFailure = failures[1];
        var expectedSaveCounts = deleteFailure is null ? ShiftTestData.WinnerSaveCounts : ShiftTestData.RaceSaveCounts;

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Order().Should().Equal(expectedSaveCounts);
    }

    [Fact]
    public async Task DeleteOverride_OfAPastDate_ThrowsValidationAndKeepsTheOverride()
    {
        var tenant = await ArrangeTodayAsync(time: "09:30", ShiftTestData.Morning);
        var yesterday = ShiftTestData.Today.AddDays(value: -1);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, yesterday, ShiftTestData.Afternoon);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, yesterday);

        var act = () => _fixture.DeleteOverrideAsync(tenant, yesterday, rowVersion);

        await act.Should().ThrowAsync<AppValidationException>();
        var day = await _fixture.ResolveDayAsync(tenant, yesterday);

        day.Source.Should().Be(DayScheduleSource.Override);
    }

    [Fact]
    public async Task DeleteOverride_OfADayOffToday_KeepsThePastAndReturnsTheRestToTheTemplate()
    {
        var tenant = await ArrangeTodayAsync(time: "14:02", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Source.Should().Be(DayScheduleSource.Override);
        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("14:05-17:00");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayThatLiesInTheFuture_DeletesTheOverride()
    {
        var tenant = await ArrangeTodayAsync(time: "06:00", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, "10:00-14:00");
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Source.Should().Be(DayScheduleSource.Template);
    }

    [Fact]
    public async Task DeleteOverride_OfTodayWithARunningBlock_JoinsTheRunningBlockWithTheTemplate()
    {
        var tenant = await ArrangeTodayAsync(time: "09:32", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, ShiftTestData.ShortMorning);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Source.Should().Be(DayScheduleSource.Override);
        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}")
            .Should()
            .Equal("09:00-12:00", "13:00-17:00");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayWithAStaleRowVersion_ThrowsConflictAndChangesNothing()
    {
        var tenant = await ArrangeTodayAsync(time: "14:02", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today);
        var staleRowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);
        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today,
            ShiftTestData.Blocks("14:05-16:00"),
            staleRowVersion);

        var act = () => _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, staleRowVersion);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(ShiftErrors.Rejections.OverrideChanged);
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("14:05-16:00");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayThatLeavesAWorkOrderOutsideTheShift_ThrowsAndChangesNothing()
    {
        var tenant = await ArrangeTodayAsync(time: "12:02", ShiftTestData.Morning);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, "08:00-16:00");
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Today,
            time: "14:00",
            minutes: 60);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        var act = () => _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var exception = (await act.Should().ThrowAsync<UnprocessableException>()).Which;
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        exception.Message.Should().Be(ShiftErrors.Rejections.WorkOrdersOutsideShift);
        exception.Extensions[ShiftChangeGuard.WorkOrdersKey]
            .Should()
            .BeAssignableTo<IReadOnlyList<AffectedWorkOrder>>()
            .Which.Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be(workOrderId);
        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("08:00-16:00");
    }

    [Fact]
    public async Task SetOverride_ThatShortensTodayUnderARunningWorkOrder_ThrowsWithTheWorkOrder()
    {
        var tenant = await ArrangeTodayAsync(time: "14:00", "08:00-17:00");
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Today,
            time: "13:30",
            minutes: 120);
        await _fixture.ChangeWorkOrderStatusAsync(workOrderId, WorkOrderStatus.InProgress);

        await AssertOutsideShiftAsync(tenant, workOrderId);
    }

    [Fact]
    public async Task SetOverride_ThatShortensTodayUnderADelayedWorkOrder_ThrowsWithTheWorkOrder()
    {
        var tenant = await ArrangeTodayAsync(time: "14:00", "08:00-17:00");
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Today,
            time: "13:30",
            minutes: 120);

        await AssertOutsideShiftAsync(tenant, workOrderId);
    }

    [Fact]
    public async Task SetOverride_ThatShortensTodayUnderACompletedWorkOrder_Succeeds()
    {
        var tenant = await ArrangeTodayAsync(time: "14:00", "08:00-17:00");
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.Today,
            time: "13:30",
            minutes: 120);
        await _fixture.CompleteWorkOrderAsync(workOrderId);

        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks("08:00-14:05"));

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("08:00-14:05");
    }

    [Fact]
    public async Task SetOverride_ThatShortensTodayAfterAWorkOrderEndedExactlyNow_Succeeds()
    {
        var tenant = await ArrangeTodayAsync(time: "14:00", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.ArrangeStagedWorkOrderAsync(tenant, ShiftTestData.Today, time: "12:00", minutes: 120);

        await _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today,
            ShiftTestData.Blocks(ShiftTestData.Morning, "13:00-14:05"));

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}")
            .Should()
            .Equal("08:00-12:00", "13:00-14:05");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayWithTouchingEndedBlocks_KeepsBothEndedBlocks()
    {
        var tenant = await ArrangeTodayAsync(time: "13:02", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, "08:00-10:00", "10:00-12:00");
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}")
            .Should()
            .Equal("08:00-12:00", "13:05-17:00");
    }

    [Fact]
    public async Task SetOverride_WithTouchingBlocks_StoresOneJoinedBlock()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        await _fixture.SetOverrideAsync(
            tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks("08:00-12:00", "12:00-13:00", "13:05-14:00"));

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}")
            .Should()
            .Equal("08:00-13:00", "13:05-14:00");
    }

    [Fact]
    public async Task SetOverride_ForTodayThatAddsABlockTouchingAnEndedBlock_StoresOneJoinedBlock()
    {
        var tenant = await ArrangeTodayAsync(time: "12:00", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, ShiftTestData.Morning);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.SetOverrideAsync(
            tenant, ShiftTestData.Today, ShiftTestData.Blocks(ShiftTestData.Morning, "12:00-17:00"), rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("08:00-17:00");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayAtTheExactEndOfABlock_KeepsTheBlockAsItIs()
    {
        var tenant = await ArrangeTodayAsync(time: "12:00", ShiftTestData.Morning, ShiftTestData.Afternoon);
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, ShiftTestData.Morning);
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}")
            .Should()
            .Equal("08:00-12:00", "13:00-17:00");
    }

    [Fact]
    public async Task DeleteOverride_OfTodayThatEqualsATemplateWithTouchingBlocks_DeletesTheOverride()
    {
        var tenant = await ArrangeTodayAsync(time: "06:00", ShiftTestData.Morning, "12:00-17:00");
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, ShiftTestData.Morning, "12:00-17:00");
        var rowVersion = await _fixture.LoadOverrideRowVersionAsync(tenant.EmployeeId, ShiftTestData.Today);

        await _fixture.DeleteOverrideAsync(tenant, ShiftTestData.Today, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Source.Should().Be(DayScheduleSource.Template);
    }

    [Fact]
    public async Task SetOverride_ForTodayFromNowOnTheGrid_JoinsOnCallWork()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "22:02"));

        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks("22:05-23:55"));

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        day.Blocks.Select(block => $"{block.Start:HH:mm}-{block.End:HH:mm}").Should().Equal("22:05-23:55");
    }

    [Fact]
    public async Task SetOverride_ForTodayThatStartsBeforeNow_ThrowsPastBlockChanged()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "22:02"));

        var act = () => _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks("22:00-23:55"));

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(ShiftErrors.Rejections.PastBlockChanged);
    }

    private async Task AssertOutsideShiftAsync(ShiftTenant tenant, Guid workOrderId)
    {
        var act = () => _fixture.SetOverrideAsync(tenant, ShiftTestData.Today, ShiftTestData.Blocks("08:00-14:05"));

        var exception = (await act.Should().ThrowAsync<UnprocessableException>()).Which;

        exception.Extensions[ShiftChangeGuard.WorkOrdersKey]
            .Should()
            .BeAssignableTo<IReadOnlyList<AffectedWorkOrder>>()
            .Which.Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be(workOrderId);
    }

    private async Task<ShiftTenant> ArrangeTodayAsync(string time, params string[] templateRanges)
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(tenant.EmployeeId, ShiftTestData.Today.AddDays(value: -30), templateRanges);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time));

        return tenant;
    }
}
