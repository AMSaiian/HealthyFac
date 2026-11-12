using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ShiftTemplateVersionTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ShiftTestData.DatesOutsideTheRange), MemberType = typeof(ShiftTestData))]
    public async Task CreateVersion_WithDateOutsideTheRange_ThrowsValidation(string caseName, int dayOffset)
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        var act = () => _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Today.AddDays(dayOffset),
            ShiftTestData.WorkWeek(ShiftTestData.Morning));

        await act.Should().ThrowAsync<AppValidationException>(because: caseName);
    }

    [Fact]
    public async Task CreateVersion_ForTomorrow_ShowsTheBlocksOnThatDay()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));

        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning, ShiftTestData.Afternoon));

        var tomorrow = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);
        var today = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);

        tomorrow.Source.Should().Be(DayScheduleSource.Template);
        tomorrow.Blocks.Should().HaveCount(expected: 2);
        today.Source.Should().Be(DayScheduleSource.None);
    }

    [Fact]
    public async Task CreateVersion_WithTakenDate_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.CreateVersionAsync(
            tenant, ShiftTestData.Tomorrow, ShiftTestData.WorkWeek(ShiftTestData.Morning));

        var act = () => _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Afternoon));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateVersion_Always_ReplacesAllBlocks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);

        await _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.ShortMorning),
            rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.InTwoDays);
        var blockCount = await _fixture.CountLiveBlocksAsync(versionId);

        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(ShiftTestData.ParseTime(time: "09:00"));
        blockCount.Should().Be(expected: 5);
    }

    [Fact]
    public async Task UpdateVersion_WithOldRowVersion_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        var oldRowVersion = await _fixture.LoadRowVersionAsync(versionId);
        await _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
            oldRowVersion);

        var act = () => _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.ShortMorning),
            oldRowVersion);

        var conflict = await act.Should().ThrowAsync<ConflictException>();
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.InTwoDays);

        conflict.WithMessage(ShiftErrors.Rejections.VersionChanged);
        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(ShiftTestData.ParseTime(time: "13:00"));
    }

    [Fact]
    public async Task UpdateVersion_ThatTookEffect_ThrowsUnprocessable()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "09:30"));

        var act = () => _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
            rowVersion);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(ShiftErrors.Rejections.VersionInEffect);
    }

    [Fact]
    public async Task DeleteVersion_ThatTookEffect_ThrowsUnprocessable()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Tomorrow,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "09:30"));

        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);

        var act = () => _fixture.DeleteVersionAsync(tenant, versionId, rowVersion);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(ShiftErrors.Rejections.VersionInEffect);
    }

    [Fact]
    public async Task DeleteVersion_ThatHasNotTakenEffect_RemovesVersionAndBlocks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));

        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);

        await _fixture.DeleteVersionAsync(tenant, versionId, rowVersion);

        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.InTwoDays);
        var blockCount = await _fixture.CountLiveBlocksAsync(versionId);

        day.Source.Should().Be(DayScheduleSource.None);
        blockCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task UpdateVersion_ThatOrphansAScheduledWorkOrder_ThrowsWithTheWorkOrderAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning, ShiftTestData.Afternoon));
        var workOrderId = await _fixture.ArrangeStagedWorkOrderAsync(
            tenant,
            ShiftTestData.InTwoDays,
            time: "14:00",
            minutes: 60);
        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);

        var act = () => _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.Morning),
            rowVersion);

        var exception = (await act.Should().ThrowAsync<UnprocessableException>()).Which;
        var day = await _fixture.ResolveDayAsync(tenant, ShiftTestData.InTwoDays);

        exception.Extensions[ShiftChangeGuard.WorkOrdersKey]
            .Should()
            .BeAssignableTo<IReadOnlyList<AffectedWorkOrder>>()
            .Which.Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be(workOrderId);
        day.Blocks.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task CreateVersion_ForTodayThatChangesAnEndedBlock_ThrowsPastBlockChanged()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "14:00"));
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);

        var act = () => _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Today,
            ShiftTestData.WorkWeek(ShiftTestData.ShortMorning, ShiftTestData.Afternoon));

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(ShiftErrors.Rejections.PastBlockChanged);
    }

    [Fact]
    public async Task CreateVersion_ForTodayWithOverrideOnToday_SucceedsAndKeepsToday()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "14:00"));
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Today, ShiftTestData.ShortMorning);

        await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.Today,
            ShiftTestData.WorkWeek(ShiftTestData.Afternoon));

        var today = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Today);
        var tomorrow = await _fixture.ResolveDayAsync(tenant, ShiftTestData.Tomorrow);

        today.Source.Should().Be(DayScheduleSource.Override);
        today.Blocks.Should().ContainSingle().Which.Start.Should().Be(ShiftTestData.ParseTime(time: "09:00"));
        tomorrow.Blocks.Should().ContainSingle().Which.Start.Should().Be(ShiftTestData.ParseTime(time: "13:00"));
    }

    [Fact]
    public async Task UpdateVersion_WithTwoParallelUpdatesOfOneRowVersion_EndsWithOneSuccessAndOneConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.UpdateVersionAsync(
                tenant,
                versionId,
                ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
                rowVersion)),
            race.RunAsync(() => _fixture.UpdateVersionAsync(
                tenant,
                versionId,
                ShiftTestData.WorkWeek(ShiftTestData.ShortMorning),
                rowVersion)));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Order().Should().Equal(ShiftTestData.RaceSaveCounts);
    }

    [Fact]
    public async Task DeleteVersion_WithStaleRowVersion_ThrowsConflictAndKeepsTheVersion()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        var staleRowVersion = await _fixture.LoadRowVersionAsync(versionId);
        await _fixture.UpdateVersionAsync(
            tenant,
            versionId,
            ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
            staleRowVersion);

        var act = () => _fixture.DeleteVersionAsync(tenant, versionId, staleRowVersion);

        var conflict = await act.Should().ThrowAsync<ConflictException>();
        var blockCount = await _fixture.CountLiveBlocksAsync(versionId);

        conflict.WithMessage(ShiftErrors.Rejections.VersionChanged);
        blockCount.Should().BeGreaterThan(expected: 0);
    }

    [Fact]
    public async Task DeleteVersion_RacingAnUpdateOfOneRowVersion_EndsWithOneSuccessAndOneConflict()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        var versionId = await _fixture.CreateVersionAsync(
            tenant,
            ShiftTestData.InTwoDays,
            ShiftTestData.WorkWeek(ShiftTestData.Morning));
        var rowVersion = await _fixture.LoadRowVersionAsync(versionId);
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.UpdateVersionAsync(
                tenant,
                versionId,
                ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
                rowVersion)),
            race.RunAsync(() => _fixture.DeleteVersionAsync(tenant, versionId, rowVersion)));

        var deleteFailure = failures[1];
        var expectedSaveCounts = deleteFailure is null ? ShiftTestData.WinnerSaveCounts : ShiftTestData.RaceSaveCounts;

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        race.SaveCounts.Order().Should().Equal(expectedSaveCounts);
    }
}
