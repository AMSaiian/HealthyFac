using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the week of the seeded shift templates and the start times of the seeded work orders. Every start time
/// lies in a block, and each order lasts 60 minutes, so no two orders of one person overlap.
/// </summary>
internal static class SeedSchedule
{
    public static readonly IReadOnlyList<Weekday> Workdays =
        [Weekday.Monday, Weekday.Tuesday, Weekday.Wednesday, Weekday.Thursday, Weekday.Friday];

    public static readonly IReadOnlyList<BlockTimes> Blocks =
    [
        new() { Start = new TimeOnly(hour: 8, minute: 0), End = new TimeOnly(hour: 12, minute: 0) },
        new() { Start = new TimeOnly(hour: 13, minute: 0), End = new TimeOnly(hour: 17, minute: 0) },
    ];

    public static readonly TimeOnly FirstSlot = new(hour: 8, minute: 0);
    public static readonly TimeOnly SecondSlot = new(hour: 9, minute: 0);
    public static readonly TimeOnly ThirdSlot = new(hour: 10, minute: 0);
    public static readonly TimeOnly AfternoonSlot = new(hour: 13, minute: 0);
}
