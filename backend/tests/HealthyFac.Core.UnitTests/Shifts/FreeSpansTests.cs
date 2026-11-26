using System.Globalization;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Core.UnitTests.Shifts.Data;

namespace HealthyFac.Core.UnitTests.Shifts;

public sealed class FreeSpansTests
{
    [Theory]
    [MemberData(nameof(FreeSpansTestData.Cases), MemberType = typeof(FreeSpansTestData))]
    public void Of_WithBlocksOccupanciesAndNow_GivesTheFreeSpansOnTheGrid(
        string caseName,
        string blocks,
        string occupied,
        string now,
        string expected
    )
    {
        var spans = FreeSpans.Of(
            FreeSpansTestData.Day(FreeSpansTestData.Date, blocks),
            FreeSpansTestData.Occupancies(FreeSpansTestData.Date, occupied),
            FreeSpansTestData.Utc(FreeSpansTestData.Date, now),
            ShiftTestData.Zone);

        FreeSpansTestData.Format(spans).Should().Be(expected, caseName);
    }

    [Fact]
    public void Of_WithACompletedWorkOrderThatFinishedLate_StartsTheSpanAtTheNextGridPoint()
    {
        var late = Occupancy.Of(
            Guid.NewGuid(),
            FreeSpansTestData.Utc(FreeSpansTestData.Date, time: "09:00"),
            TimeSpan.FromHours(value: 1),
            FreeSpansTestData.Utc(FreeSpansTestData.Date, time: "10:07"));

        var spans = FreeSpans.Of(
            FreeSpansTestData.Day(FreeSpansTestData.Date, blocks: "08:00-12:00"),
            [late],
            FreeSpansTestData.Utc(FreeSpansTestData.Date, time: "07:00"),
            ShiftTestData.Zone);

        FreeSpansTestData.Format(spans).Should().Be(expected: "08:00-09:00,10:10-12:00");
    }

    [Fact]
    public void Of_ForAPastDate_GivesNoSpan()
    {
        var spans = FreeSpans.Of(
            FreeSpansTestData.Day(FreeSpansTestData.Date, blocks: "08:00-12:00"),
            [],
            FreeSpansTestData.Utc(FreeSpansTestData.Date.AddDays(value: 1), time: "07:00"),
            ShiftTestData.Zone);

        spans.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(FreeSpansTestData.DstSpans), MemberType = typeof(FreeSpansTestData))]
    public void LongestMinutes_OverAClockChange_CountsRealMinutes(
        string date,
        string start,
        string end,
        int expectedMinutes
    )
    {
        var day = DateOnly.Parse(date, CultureInfo.InvariantCulture);

        var spans = FreeSpans.Of(
            FreeSpansTestData.Day(day, $"{start}-{end}"),
            [],
            FreeSpansTestData.Utc(day.AddDays(value: -1), time: "12:00"),
            ShiftTestData.Zone);

        FreeSpans.LongestMinutes(spans).Should().Be(expectedMinutes);
    }

    [Fact]
    public void LongestMinutes_WithoutSpans_IsZero()
    {
        FreeSpans.LongestMinutes([]).Should().Be(expected: 0);
    }
}
