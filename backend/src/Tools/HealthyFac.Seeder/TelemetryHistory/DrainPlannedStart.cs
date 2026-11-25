using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// Chooses the planned start of the work order of a drain. Core accepts a start on its grid, not before the
/// assignment, with the whole slot inside one work block of the assignee.
/// </summary>
public static class DrainPlannedStart
{
    /// <summary>
    /// Takes the maintenance start, rounded up to the grid (and not before the assignment), when the slot fits one
    /// block and overlaps no busy slot. Else it takes the earliest grid moment after it with that property.
    /// </summary>
    /// <remarks>
    /// A fitting moment is the start itself, the start of a block, or the end of a busy slot, so the search tests
    /// only these moments.
    /// </remarks>
    /// <param name="maintenanceStartUtc">The start of the maintenance.</param>
    /// <param name="estimate">The planned working time.</param>
    /// <param name="assignedAtUtc">The moment of the assignment. The planned start never lies before it.</param>
    /// <param name="blocks">The work blocks of the assignee from the day of the assignment on, sorted by start.</param>
    /// <param name="busy">The slots of the other work orders of the assignee.</param>
    /// <returns>The planned start as a UTC instant on the grid, or <see langword="null"/> when no block fits.</returns>
    public static DateTime? Choose(
        DateTime maintenanceStartUtc,
        TimeSpan estimate,
        DateTime assignedAtUtc,
        IReadOnlyList<DayBlock> blocks,
        IReadOnlyList<Occupancy> busy
    )
    {
        var earliest =
            SlotRules.RoundUpToGrid(maintenanceStartUtc > assignedAtUtc ? maintenanceStartUtc : assignedAtUtc);
        var candidates = blocks.Select(block => block.StartUtc)
            .Concat(busy.Select(slot => slot.EndUtc))
            .Select(SlotRules.RoundUpToGrid)
            .Where(moment => moment > earliest)
            .Prepend(earliest)
            .Distinct()
            .OrderBy(moment => moment);
        foreach (var candidate in candidates)
        {
            var slot = Occupancy.Of(Guid.Empty, candidate, estimate, completedAtUtc: null);
            if (blocks.Any(block => block.StartUtc <= candidate && slot.EndUtc <= block.EndUtc)
                && !SlotRules.Overlaps(slot, busy))
            {
                return candidate;
            }
        }

        return null;
    }
}
