using System.Globalization;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.UnitTests.Shifts.Data;

namespace HealthyFac.Core.UnitTests.Shifts;

public sealed class DayScheduleResolverTests
{
    [Fact]
    public void Resolve_WithOverride_UsesOverrideBlocks()
    {
        var version = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 1, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"));
        var shiftOverride = ShiftTestData.Override(
            ShiftTestData.Monday, ShiftTestData.Times(start: "10:00", end: "11:00"));

        var day = DayScheduleResolver.Resolve(ShiftTestData.Monday, ShiftTestData.Zone, [version], shiftOverride);

        day.Source.Should().Be(DayScheduleSource.Override);
        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(new TimeOnly(hour: 10, minute: 0));
    }

    [Fact]
    public void Resolve_WithEmptyOverride_IsDayOffFromOverride()
    {
        var version = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 1, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"));
        var shiftOverride = ShiftTestData.Override(ShiftTestData.Monday);

        var day = DayScheduleResolver.Resolve(ShiftTestData.Monday, ShiftTestData.Zone, [version], shiftOverride);

        day.Source.Should().Be(DayScheduleSource.Override);
        day.Blocks.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithTemplateOnly_UsesBlocksOfTheWeekdayInStartOrder()
    {
        var version = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 1, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "13:00", end: "17:00"),
            ShiftTestData.Template(Weekday.Tuesday, start: "06:00", end: "07:00"),
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"));

        var day = DayScheduleResolver.Resolve(ShiftTestData.Monday, ShiftTestData.Zone, [version], shiftOverride: null);

        day.Source.Should().Be(DayScheduleSource.Template);
        day.Blocks.Select(block => block.Start)
            .Should()
            .Equal(new TimeOnly(hour: 8, minute: 0), new TimeOnly(hour: 13, minute: 0));
    }

    [Fact]
    public void Resolve_WithDateBeforeFirstVersion_HasNoSourceAndNoBlocks()
    {
        var version = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 6, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"));

        var day = DayScheduleResolver.Resolve(ShiftTestData.Monday, ShiftTestData.Zone, [version], shiftOverride: null);

        day.Source.Should().Be(DayScheduleSource.None);
        day.Blocks.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithTwoVersions_UsesTheLatestOneThatIsInEffect()
    {
        var first = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 1, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"));
        var second = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 4, day: 5),
            ShiftTestData.Template(Weekday.Monday, start: "09:00", end: "10:00"));
        var third = ShiftTestData.Version(
            new DateOnly(year: 2027, month: 5, day: 1),
            ShiftTestData.Template(Weekday.Monday, start: "10:00", end: "11:00"));

        var day = DayScheduleResolver.Resolve(
            ShiftTestData.Monday, ShiftTestData.Zone, [third, first, second], shiftOverride: null);

        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(new TimeOnly(hour: 9, minute: 0));
    }

    [Theory]
    [MemberData(nameof(ShiftTestData.DstBlockLengths), MemberType = typeof(ShiftTestData))]
    public void Resolve_OnDstDate_UsesLocalOffsetsForTheBlockLength(string date, string start, string end, int hours)
    {
        var day = DateOnly.ParseExact(date, ShiftTestData.DateFormat, CultureInfo.InvariantCulture);
        var shiftOverride = ShiftTestData.Override(day, ShiftTestData.Times(start, end));

        var resolved = DayScheduleResolver.Resolve(day, ShiftTestData.Zone, [], shiftOverride);

        (resolved.Blocks.Single().EndUtc - resolved.Blocks.Single().StartUtc).Should().Be(TimeSpan.FromHours(hours));
    }
}
