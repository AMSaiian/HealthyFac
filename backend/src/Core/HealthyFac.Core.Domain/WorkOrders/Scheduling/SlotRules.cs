using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Domain.WorkOrders.Scheduling;

/// <summary>
/// The rules that a slot of a work order must meet. Every function works on UTC instants and half-open intervals.
/// </summary>
public static class SlotRules
{
    /// <summary>
    /// The step of the planned start. The offset of a zone is a multiple of 5 minutes, so a start on the grid in UTC is
    /// on the grid in local time too.
    /// </summary>
    public static readonly TimeSpan GridStep = TimeSpan.FromMinutes(value: 5);

    /// <summary>
    /// Tells whether the slot lies inside one block of the day. A slot across a gap between two blocks does not fit.
    /// </summary>
    /// <param name="slot">The slot to test.</param>
    /// <param name="day">The resolved day of the slot start.</param>
    /// <returns><see langword="true"/> when one block holds the whole slot.</returns>
    public static bool FitsOneBlock(Occupancy slot, DaySchedule day) =>
        day.Blocks.Any(block => block.StartUtc <= slot.StartUtc && slot.EndUtc <= block.EndUtc);

    /// <summary>
    /// Tells whether the slot overlaps the occupancy of another work order. A slot may start exactly where another
    /// ends. The occupancy of the work order of the slot itself does not count.
    /// </summary>
    /// <param name="slot">The slot to test.</param>
    /// <param name="occupancies">The occupancies of the other scheduled work orders of the assignee.</param>
    /// <returns><see langword="true"/> when a time interval is shared.</returns>
    public static bool Overlaps(Occupancy slot, IEnumerable<Occupancy> occupancies) =>
        occupancies.Any(other => other.WorkOrderId != slot.WorkOrderId
            && other.StartUtc < slot.EndUtc
            && slot.StartUtc < other.EndUtc);

    /// <summary>
    /// Rounds an instant up to the next point of <see cref="GridStep"/>. An instant on the grid stays as it is.
    /// </summary>
    /// <param name="instantUtc">The instant to round.</param>
    /// <returns>The rounded instant with <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime RoundUpToGrid(DateTime instantUtc)
    {
        var remainder = instantUtc.Ticks % GridStep.Ticks;
        var ticks = remainder == 0 ? instantUtc.Ticks : instantUtc.Ticks + GridStep.Ticks - remainder;

        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
