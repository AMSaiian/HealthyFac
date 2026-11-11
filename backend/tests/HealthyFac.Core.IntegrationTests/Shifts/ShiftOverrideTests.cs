using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ShiftOverrideTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SetOverride_ForPastDate_ThrowsValidation()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var act = () => _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today.AddDays(value: -1),
            ShiftTestData.Blocks(ShiftTestData.Morning));

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task SetOverride_BeyondOneYear_ThrowsValidation()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var act = () => _fixture.SetOverrideAsync(
            tenant,
            ShiftTestData.Today.AddDays(ShiftValidationConstants.MaxDaysAhead + 1),
            ShiftTestData.Blocks(ShiftTestData.Morning));

        await act.Should().ThrowAsync<AppValidationException>();
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
}
