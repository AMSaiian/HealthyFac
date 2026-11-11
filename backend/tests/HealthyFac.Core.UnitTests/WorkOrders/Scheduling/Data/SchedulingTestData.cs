using System.Globalization;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

public static class SchedulingTestData
{
    public const string ZoneId = "Europe/Kyiv";
    public const string TimeFormat = "HH:mm";

    private static readonly DateOnly Date = new(year: 2027, month: 4, day: 12);

    public static TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static DaySchedule Day() =>
        DayScheduleResolver.Resolve(
            Date,
            Zone,
            [],
            new ShiftOverride(
                Guid.Empty,
                Date,
                [
                    new BlockTimes
                        { Start = new TimeOnly(hour: 8, minute: 0), End = new TimeOnly(hour: 12, minute: 0) },
                    new BlockTimes
                        { Start = new TimeOnly(hour: 13, minute: 0), End = new TimeOnly(hour: 17, minute: 0) },
                ]));

    public static DateTime At(string time) =>
        LocalTimes.ToUtc(
            Date.ToDateTime(TimeOnly.ParseExact(time, TimeFormat, CultureInfo.InvariantCulture)),
            Zone);

    public static DateTime NextDayAt(string time) =>
        LocalTimes.ToUtc(
            Date.AddDays(value: 1).ToDateTime(TimeOnly.ParseExact(time, TimeFormat, CultureInfo.InvariantCulture)),
            Zone);

    public static Guid Id(int number) => new(number, b: 0, c: 0, d: 0, e: 0, f: 0, g: 0, h: 0, i: 0, j: 0, k: 0);

    public static Occupancy Slot(int number, string start, int minutes) =>
        Occupancy.Of(Id(number), At(start), TimeSpan.FromMinutes(minutes), completedAtUtc: null);

    public static SlideWorkOrder Order(int number, WorkOrderStatus status, string start, int minutes) =>
        new()
        {
            Id = Id(number),
            Status = status,
            PlannedStartUtc = At(start),
            Estimate = TimeSpan.FromMinutes(minutes),
        };
}
