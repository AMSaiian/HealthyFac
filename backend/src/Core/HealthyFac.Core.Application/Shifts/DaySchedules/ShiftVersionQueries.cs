using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Application.Shifts.DaySchedules;

public static class ShiftVersionQueries
{
    /// <summary>
    /// Keeps the template versions in force for the dates of a closed range: the version that starts inside the range
    /// and the newest version that starts on or before the first date, per employee.
    /// </summary>
    /// <remarks>
    /// A plain filter on <c>EffectiveFrom</c> is wrong: the version that started before the range still gives the
    /// blocks of every date until the next version starts. The subquery reads the same source, so the soft delete
    /// filter and any employee filter of the source apply to it too.
    /// </remarks>
    /// <param name="versions">The versions to filter.</param>
    /// <param name="from">The first date of the range.</param>
    /// <param name="to">The last date of the range.</param>
    /// <returns>The versions in force for at least one date of the range.</returns>
    public static IQueryable<ShiftTemplateVersion> InForceBetween(
        this IQueryable<ShiftTemplateVersion> versions,
        DateOnly from,
        DateOnly to
    ) =>
        versions.Where(version => version.EffectiveFrom <= to
            && !versions.Any(other => other.EmployeeId == version.EmployeeId
                && other.EffectiveFrom > version.EffectiveFrom
                && other.EffectiveFrom <= from));
}
