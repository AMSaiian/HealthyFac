using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Core.Domain.Shifts.DaySchedules;

/// <summary>
/// Finds the free time of a day: the work blocks without the occupancy of scheduled work orders and without the
/// past.
/// </summary>
public static class FreeSpans
{
    /// <summary>
    /// Gives the free spans of a day. Each block starts at the later of its start and now on the grid, and loses every
    /// occupancy. Each span start is rounded up and each span end is rounded down to the grid step (a zone offset is a
    /// multiple of the step, so the UTC and the local grid agree). A span shorter than one step is dropped.
    /// </summary>
    /// <param name="day">The resolved day.</param>
    /// <param name="occupancies">The occupancies of the other scheduled work orders of the employee.</param>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <param name="zone">The zone of the company, for the local bounds.</param>
    /// <returns>The free spans in time order.</returns>
    public static IReadOnlyList<FreeSpan> Of(
        DaySchedule day,
        IReadOnlyCollection<Occupancy> occupancies,
        DateTime nowUtc,
        TimeZoneInfo zone
    )
    {
        var nowOnGridUtc = SlotRules.RoundUpToGrid(nowUtc);
        var ordered = occupancies.OrderBy(occupancy => occupancy.StartUtc).ToList();

        return day.Blocks
            .SelectMany(block => Subtract(
                block.StartUtc > nowOnGridUtc ? block.StartUtc : nowOnGridUtc, block.EndUtc, ordered))
            .Select(span => (Start: SlotRules.RoundUpToGrid(span.Start), End: SlotRules.RoundDownToGrid(span.End)))
            .Where(span => span.End - span.Start >= SlotRules.GridStep)
            .OrderBy(span => span.Start)
            .Select(span => new FreeSpan
            {
                StartUtc = span.Start,
                EndUtc = span.End,
                StartLocal = ToLocal(span.Start, zone),
                EndLocal = ToLocal(span.End, zone),
            })
            .ToList();
    }

    /// <summary>
    /// Gives the length of the longest span in real minutes (a span over a clock change counts its real length).
    /// </summary>
    /// <param name="spans">The free spans.</param>
    /// <returns>The longest length in minutes, or <c>0</c> without spans.</returns>
    public static int LongestMinutes(IReadOnlyCollection<FreeSpan> spans) =>
        spans.Count == 0 ? 0 : (int)spans.Max(span => (span.EndUtc - span.StartUtc).TotalMinutes);

    private static IEnumerable<(DateTime Start, DateTime End)> Subtract(
        DateTime startUtc,
        DateTime endUtc,
        IEnumerable<Occupancy> ordered
    )
    {
        var cursor = startUtc;
        foreach (var occupancy in
            ordered.Where(occupancy => occupancy.EndUtc > startUtc && occupancy.StartUtc < endUtc))
        {
            if (occupancy.StartUtc > cursor)
            {
                yield return (cursor, occupancy.StartUtc);
            }

            cursor = occupancy.EndUtc > cursor ? occupancy.EndUtc : cursor;
        }

        if (cursor < endUtc)
        {
            yield return (cursor, endUtc);
        }
    }

    private static DateTime ToLocal(DateTime instantUtc, TimeZoneInfo zone) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(instantUtc, zone), DateTimeKind.Unspecified);
}
