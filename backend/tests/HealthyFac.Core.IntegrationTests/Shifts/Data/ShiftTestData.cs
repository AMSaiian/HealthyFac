using System.Globalization;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.IntegrationTests.Shifts.Data;

public static class ShiftTestData
{
    public const string ZoneId = "Europe/Kyiv";
    public const string CompanyName = "Shift Plant";
    public const string EmployeeEmail = "shift.worker@plant.com";
    public const string EmployeeFirstName = "Sam";
    public const string EmployeeLastName = "Shifter";
    public const string OtherFirstName = "Olga";
    public const string OtherEmail = "shift.other@plant.com";
    public const string DeactivatedEmail = "shift.gone@plant.com";
    public const string TimeFormat = "HH:mm";
    public const string Morning = "08:00-12:00";
    public const string Afternoon = "13:00-17:00";
    public const string ShortMorning = "09:00-11:00";
    public const string NightBlock = "02:00-05:00";
    public const int RaceParticipantCount = 2;

    /// <summary>The first call saves once. The loser saves twice: the failed save and the save of its retry.</summary>
    public static readonly IReadOnlyList<int> RaceSaveCounts = [1, 2];

    /// <summary>A taken unique key is no transient failure: both calls save once, the loser gets a conflict.</summary>
    public static readonly IReadOnlyList<int> UniqueRaceSaveCounts = [1, 1];

    /// <summary>The save counts of a delete race: the loser fails at its second read, before a second save.</summary>
    public static readonly IReadOnlyList<int> WinnerSaveCounts = [1, 1];

    /// <summary>The local "now" of the tests, a Monday after the spring DST change: 09:30 in Kyiv.</summary>
    public static readonly DateTime NowLocal = new(year: 2027, month: 4, day: 12, hour: 9, minute: 30, second: 0);

    public static readonly DateOnly Today = new(year: 2027, month: 4, day: 12);
    public static readonly DateOnly Tomorrow = Today.AddDays(value: 1);
    public static readonly DateOnly InTwoDays = Today.AddDays(value: 2);

    public static TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static DateTimeOffset NowAt(string time) =>
        new(LocalTimes.ToUtc(Today.ToDateTime(ParseTime(time)), Zone), TimeSpan.Zero);

    public static DateTimeOffset NowOn(DateOnly date, string time) =>
        new(LocalTimes.ToUtc(date.ToDateTime(ParseTime(time)), Zone), TimeSpan.Zero);

    public static TimeOnly ParseTime(string time) =>
        TimeOnly.ParseExact(time, TimeFormat, CultureInfo.InvariantCulture);

    public static ShiftBlockInput Block(string range)
    {
        var parts = range.Split(separator: '-');

        return new ShiftBlockInput { Start = ParseTime(parts[0]), End = ParseTime(parts[1]) };
    }

    public static IReadOnlyList<ShiftBlockInput> Blocks(params string[] ranges) => ranges.Select(Block).ToList();

    public static IReadOnlyList<ShiftTemplateDayInput> WorkWeek(params string[] ranges) =>
        Weekday.All
            .Select(weekday => new ShiftTemplateDayInput
            {
                Weekday = weekday,
                Blocks = weekday == Weekday.Saturday || weekday == Weekday.Sunday ? [] : Blocks(ranges),
            })
            .ToList();

    public static IEnumerable<(Weekday Weekday, BlockTimes Times)> WorkWeekBlocks(params string[] ranges) =>
        ShiftTemplateDayInput.ToTemplateBlocks(WorkWeek(ranges));

    public static DateTime LocalStart(DateOnly date, string time) => date.ToDateTime(ParseTime(time));
}
