using System.Globalization;
using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.UnitTests.Shifts.Data;

public static class ShiftTestData
{
    public const string ZoneId = "Europe/Kyiv";
    public const string TimeFormat = "HH:mm";
    public const string DateFormat = "yyyy-MM-dd";

    public static readonly Guid EmployeeId = Guid.Parse(input: "6fc273b8-d094-41e4-9377-8f90bcde5167");
    public static readonly DateOnly SpringDstDate = new(year: 2027, month: 3, day: 28);
    public static readonly DateOnly AutumnDstDate = new(year: 2027, month: 10, day: 31);
    public static readonly DateOnly Monday = new(year: 2027, month: 4, day: 12);
    public static readonly DateOnly Tuesday = new(year: 2027, month: 4, day: 13);

    private static readonly DateTime BaseDay = new(year: 2027, month: 4, day: 12, hour: 0, minute: 0, second: 0);

    public static TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static BlockTimes Times(string start, string end) =>
        new()
        {
            Start = TimeOnly.ParseExact(start, TimeFormat, CultureInfo.InvariantCulture),
            End = TimeOnly.ParseExact(end, TimeFormat, CultureInfo.InvariantCulture),
        };

    public static (Weekday, BlockTimes) Template(Weekday weekday, string start, string end) =>
        (weekday, Times(start, end));

    public static ShiftTemplateVersion Version(DateOnly effectiveFrom, params (Weekday, BlockTimes)[] blocks) =>
        new(EmployeeId, effectiveFrom, blocks);

    public static ShiftOverride Override(DateOnly date, params BlockTimes[] blocks) =>
        new(EmployeeId, date, blocks);

    public static IReadOnlyCollection<DayBlock> Blocks(string blocks) =>
        blocks.Split(separator: ',', StringSplitOptions.RemoveEmptyEntries)
            .Select(block =>
            {
                var parts = block.Split(separator: '-');

                return new DayBlock
                {
                    Start = TimeOnly.ParseExact(parts[0], TimeFormat, CultureInfo.InvariantCulture),
                    End = TimeOnly.ParseExact(parts[1], TimeFormat, CultureInfo.InvariantCulture),
                    StartUtc = BaseDay.Add(
                        TimeSpan.ParseExact(parts[0], format: "hh\\:mm", CultureInfo.InvariantCulture)),
                    EndUtc =
                        BaseDay.Add(TimeSpan.ParseExact(parts[1], format: "hh\\:mm", CultureInfo.InvariantCulture)),
                };
            })
            .ToList();

    public static DateTime Now(string time) =>
        BaseDay.Add(TimeSpan.ParseExact(time, format: "hh\\:mm", CultureInfo.InvariantCulture));

    public static TheoryData<string, string, string, int> DstBlockLengths => new()
    {
        { "2027-03-28", "08:00", "12:00", 4 },
        { "2027-03-28", "02:00", "05:00", 2 },
        { "2027-10-31", "02:00", "05:00", 4 },
    };

    public static TheoryData<string, string, string> ChangedPastCases => new()
    {
        { "09:00-12:00,13:00-17:00", "09:00-11:00,13:00-17:00", "12:30" },
        { "09:00-12:00,13:00-17:00", "13:00-17:00", "12:30" },
        { "09:00-12:00,13:00-17:00", "09:00-12:00,13:00-17:00,12:10-12:20", "12:30" },
        { "09:00-12:00,13:00-17:00", "09:30-12:00,13:00-17:00", "10:00" },
        { "09:00-12:00,13:00-17:00", "09:00-09:30,13:00-17:00", "10:00" },
        { "09:00-12:00,13:00-17:00", "09:00-11:00,13:00-17:00", "12:00" },
        { "12:00-14:00", "12:05-14:00", "12:00" },
    };

    public static TheoryData<string, string, string> KeptPastCases => new()
    {
        { "09:00-12:00,13:00-17:00", "09:00-10:05,13:00-17:00", "10:02" },
        { "09:00-12:00,13:00-17:00", "09:00-12:00,14:00-18:00", "10:00" },
        { "09:00-12:00,13:00-17:00", "09:00-12:00,13:00-17:00,18:00-19:00", "10:00" },
        { "09:00-12:00,13:00-17:00", "09:00-12:00,13:00-17:00", "12:00" },
        { "12:00-14:00", "12:00-13:00", "12:00" },
        { "09:00-12:00", "09:00-12:00,10:00-11:00", "10:00" },
    };
}
