using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// The template versions and the overrides of one employee, with the zone of the company. A use case changes the
/// lists in memory with the entities, then resolves the days before and after the change.
/// </summary>
public sealed class EmployeeShifts
{
    public required TimeZoneInfo Zone { get; init; }

    public required List<ShiftTemplateVersion> Versions { get; init; }

    public required List<ShiftOverride> Overrides { get; init; }

    /// <summary>
    /// Gives the local date of the company at an instant.
    /// </summary>
    /// <param name="instantUtc">The UTC instant.</param>
    /// <returns>The local date in the zone of the company.</returns>
    public DateOnly LocalDateOf(DateTime instantUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instantUtc, Zone));

    /// <summary>
    /// Resolves every date of a closed range with the current lists.
    /// </summary>
    /// <param name="from">The first date.</param>
    /// <param name="to">The last date.</param>
    /// <returns>One resolved day for each date, in date order.</returns>
    public IReadOnlyList<DaySchedule> Resolve(DateOnly from, DateOnly to)
    {
        var days = new List<DaySchedule>();
        for (var date = from; date <= to; date = date.AddDays(value: 1))
        {
            var shiftOverride = Overrides.SingleOrDefault(candidate => candidate.Date == date);
            days.Add(DayScheduleResolver.Resolve(date, Zone, Versions, shiftOverride));
        }

        return days;
    }
}
