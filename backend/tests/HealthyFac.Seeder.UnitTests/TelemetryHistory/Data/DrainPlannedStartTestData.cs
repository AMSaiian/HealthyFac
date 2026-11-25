using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

public static class DrainPlannedStartTestData
{
    public static readonly TimeSpan Estimate = TimeSpan.FromHours(hours: 2);

    /// <summary>The blocks of 2 days: 08:00-12:00 and 13:00-17:00 on day 2, 08:00-12:00 on day 3.</summary>
    public static IReadOnlyList<DayBlock> Blocks =>
    [
        Block(day: 2, startHour: 8, endHour: 12),
        Block(day: 2, startHour: 13, endHour: 17),
        Block(day: 3, startHour: 8, endHour: 12),
    ];

    public static DateTime At(int day, int hour, int minute) =>
        new(year: 2027, month: 6, day, hour, minute, second: 0, DateTimeKind.Utc);

    public static Occupancy Busy(int day, int startHour, int endHour) =>
        Occupancy.Of(
            Guid.NewGuid(),
            At(day, startHour, minute: 0),
            TimeSpan.FromHours(endHour - startHour),
            completedAtUtc: null);

    private static DayBlock Block(int day, int startHour, int endHour) =>
        new()
        {
            Start = new TimeOnly(startHour, minute: 0),
            End = new TimeOnly(endHour, minute: 0),
            StartUtc = At(day, startHour, minute: 0),
            EndUtc = At(day, endHour, minute: 0),
        };
}
