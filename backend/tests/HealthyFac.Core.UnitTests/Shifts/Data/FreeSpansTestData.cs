using System.Globalization;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.UnitTests.Shifts.Data;

public static class FreeSpansTestData
{
    private const string TimeFormat = "HH:mm";
    private const string Template = "08:00-12:00,13:00-17:00";

    public static readonly DateOnly Date = ShiftTestData.Monday;

    public static TheoryData<string, string, string, string, string> Cases => new()
    {
        { "FreeDayGivesOneSpanPerBlock", Template, "", "07:00", Template },
        { "WorkOrderSplitsTheFirstBlock", Template, "09:00-10:00", "07:00", "08:00-09:00,10:00-12:00,13:00-17:00" },
        { "TodayStartsAtNowOnTheGrid", Template, "", "09:32", "09:35-12:00,13:00-17:00" },
        { "ThreeMinuteSpansAreDropped", Template, "08:03-11:57", "07:00", "13:00-17:00" },
        {
            "OffGridOccupancyRoundsTheSpanEndDown", Template, "09:03-10:00", "07:00",
            "08:00-09:00,10:00-12:00,13:00-17:00"
        },
        { "NowAfterTheLastBlockGivesNone", Template, "", "18:00", "" },
    };

    public static TheoryData<string, string, string, int> DstSpans => new()
    {
        { "2027-03-28", "02:00", "05:00", 120 },
        { "2027-10-31", "02:00", "05:00", 240 },
    };

    public static DaySchedule Day(DateOnly date, string blocks) =>
        DayScheduleResolver.Resolve(
            date,
            ShiftTestData.Zone,
            [],
            ShiftTestData.Override(
                date,
                blocks.Split(separator: ',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(range => range.Split(separator: '-'))
                    .Select(parts => ShiftTestData.Times(parts[0], parts[1]))
                    .ToArray()));

    public static IReadOnlyCollection<Occupancy> Occupancies(DateOnly date, string ranges) =>
        ranges.Split(separator: ',', StringSplitOptions.RemoveEmptyEntries)
            .Select(range => range.Split(separator: '-'))
            .Select(parts => Occupancy.Of(
                Guid.NewGuid(),
                Utc(date, parts[0]),
                Utc(date, parts[1]) - Utc(date, parts[0]),
                completedAtUtc: null))
            .ToList();

    public static DateTime Utc(DateOnly date, string time) =>
        LocalTimes.ToUtc(
            date.ToDateTime(TimeOnly.ParseExact(time, TimeFormat, CultureInfo.InvariantCulture)), ShiftTestData.Zone);

    public static string Format(IEnumerable<FreeSpan> spans) =>
        string.Join(separator: ',', spans.Select(span => $"{span.StartLocal:HH:mm}-{span.EndLocal:HH:mm}"));
}
