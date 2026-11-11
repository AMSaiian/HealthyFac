using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Domain.WorkOrders.Scheduling;

/// <summary>
/// Plans how the later work orders of the day move when a work order completes after its planned end.
/// </summary>
public static class CompletionSlide
{
    /// <summary>
    /// Plans the moves. The slide runs only when the completion moment <c>C</c> is after the planned end <c>E</c>
    /// of the completed work order. Its scope is the other work orders of the same assignee on the same local date
    /// <c>D</c>, at or after the planned start of the completed one, in planned start order. Work orders in
    /// progress or completed are fixed and keep their slot. Staged, blocked, and rejected ones are movable. The
    /// cursor starts at <c>C</c>. For each movable work order, the earliest start on the grid at or after the later
    /// of its own start and the cursor is the candidate, and the slot must fit one block of <c>D</c> and overlap no
    /// fixed work order and no work order that this slide placed. A candidate that differs from the own start is a
    /// move, and the cursor goes to its end. A work order without a fitting start goes to the backlog, and the
    /// cursor stays. A completion on a later local date than <c>D</c> sends every movable work order of <c>D</c> to
    /// the backlog.
    /// </summary>
    /// <param name="completed">The work order that completes.</param>
    /// <param name="completedAtUtc">The completion moment.</param>
    /// <param name="scheduled">
    /// The scheduled work orders of the assignee with a planned start on <paramref name="day"/>, without
    /// <paramref name="completed"/>.
    /// </param>
    /// <param name="day">The resolved day <c>D</c> of the planned start of the completed work order.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <returns>The moves, in the order of the work orders. A work order that keeps its start has no move.</returns>
    public static IReadOnlyList<SlideMove> Plan(
        SlideWorkOrder completed,
        DateTime completedAtUtc,
        IReadOnlyCollection<SlideWorkOrder> scheduled,
        DaySchedule day,
        TimeZoneInfo zone
    )
    {
        var plannedEndUtc = completed.PlannedStartUtc + completed.Estimate;
        if (completedAtUtc <= plannedEndUtc)
        {
            return [];
        }

        var movable = scheduled
            .Where(order => IsMovable(order) && order.PlannedStartUtc >= completed.PlannedStartUtc)
            .OrderBy(order => order.PlannedStartUtc)
            .ThenBy(order => order.Id)
            .ToList();
        var completedLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(completedAtUtc, zone));
        if (completedLocalDate > day.Date)
        {
            return movable.Select(order => new SlideMove { WorkOrderId = order.Id }).ToList();
        }

        var occupied = scheduled
            .Where(order => !IsMovable(order))
            .Select(order => Occupancy.Of(order.Id, order.PlannedStartUtc, order.Estimate, order.CompletedAtUtc))
            .ToList();
        occupied.Add(Occupancy.Of(completed.Id, completed.PlannedStartUtc, completed.Estimate, completedAtUtc));

        var moves = new List<SlideMove>();
        var cursorUtc = completedAtUtc;
        foreach (var order in movable)
        {
            var fromUtc = order.PlannedStartUtc > cursorUtc ? order.PlannedStartUtc : cursorUtc;
            var startUtc = FindStart(SlotRules.RoundUpToGrid(fromUtc), order, day, occupied);
            if (startUtc is not { } found)
            {
                moves.Add(new SlideMove { WorkOrderId = order.Id });

                continue;
            }

            occupied.Add(Occupancy.Of(order.Id, found, order.Estimate, completedAtUtc: null));
            cursorUtc = found + order.Estimate;
            if (found != order.PlannedStartUtc)
            {
                moves.Add(new SlideMove { WorkOrderId = order.Id, NewStartUtc = found });
            }
        }

        return moves;
    }

    private static bool IsMovable(SlideWorkOrder order) =>
        order.Status != WorkOrderStatus.InProgress && order.Status != WorkOrderStatus.Completed;

    private static DateTime? FindStart(
        DateTime fromUtc,
        SlideWorkOrder order,
        DaySchedule day,
        IReadOnlyCollection<Occupancy> occupied
    )
    {
        foreach (var block in day.Blocks)
        {
            var candidateUtc = SlotRules.RoundUpToGrid(fromUtc > block.StartUtc ? fromUtc : block.StartUtc);
            while (candidateUtc + order.Estimate <= block.EndUtc)
            {
                var slot = Occupancy.Of(order.Id, candidateUtc, order.Estimate, completedAtUtc: null);
                var clashEndUtc = occupied
                    .Where(other => other.StartUtc < slot.EndUtc && slot.StartUtc < other.EndUtc)
                    .Select(other => (DateTime?)other.EndUtc)
                    .Max();
                if (clashEndUtc is not { } clashEnd)
                {
                    return candidateUtc;
                }

                candidateUtc = SlotRules.RoundUpToGrid(clashEnd);
            }
        }

        return null;
    }
}
