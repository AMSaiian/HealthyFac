using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// Resolves the work blocks of an employee on a local date.
/// </summary>
public static class DayScheduleResolver
{
    /// <summary>
    /// Resolves a date: the override of the date wins (maybe without blocks), else the version with the greatest
    /// <see cref="ShiftTemplateVersion.EffectiveFrom"/> up to the date gives the blocks of the weekday, else the day
    /// has no blocks. Each block gets its UTC instants with <see cref="LocalTimes.ToUtc"/>.
    /// </summary>
    /// <param name="date">The local date of the company.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <param name="versions">The template versions of the employee.</param>
    /// <param name="shiftOverride">The override of the date, or <see langword="null"/>.</param>
    /// <returns>The resolved day.</returns>
    public static DaySchedule Resolve(
        DateOnly date,
        TimeZoneInfo zone,
        IEnumerable<ShiftTemplateVersion> versions,
        ShiftOverride? shiftOverride
    )
    {
        if (shiftOverride is not null)
        {
            return Create(
                date,
                zone,
                DayScheduleSource.Override,
                shiftOverride.Blocks.Select(block => new BlockTimes { Start = block.Start, End = block.End }));
        }

        var version = versions
            .Where(candidate => candidate.EffectiveFrom <= date)
            .MaxBy(candidate => candidate.EffectiveFrom);
        if (version is null)
        {
            return Create(date, zone, DayScheduleSource.None, []);
        }

        var weekday = Weekday.From(date.DayOfWeek);

        return Create(
            date,
            zone,
            DayScheduleSource.Template,
            version.Blocks
                .Where(block => block.Weekday == weekday)
                .Select(block => new BlockTimes { Start = block.Start, End = block.End }));
    }

    private static DaySchedule Create(
        DateOnly date,
        TimeZoneInfo zone,
        DayScheduleSource source,
        IEnumerable<BlockTimes> times
    ) =>
        new()
        {
            Date = date,
            Source = source,
            Blocks = times
                .OrderBy(block => block.Start)
                .Select(block => new DayBlock
                {
                    Start = block.Start,
                    End = block.End,
                    StartUtc = LocalTimes.ToUtc(date.ToDateTime(block.Start), zone),
                    EndUtc = LocalTimes.ToUtc(date.ToDateTime(block.End), zone),
                })
                .ToList(),
        };
}
