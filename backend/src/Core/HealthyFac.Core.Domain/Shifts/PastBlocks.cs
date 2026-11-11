namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// Protects the part of today that already happened: an ended block, and the start of a running block.
/// </summary>
public static class PastBlocks
{
    /// <summary>
    /// Compares the blocks of today before and after a change. An ended block (<c>End &lt;= now</c>) must stay with the
    /// same start and end. A running block (<c>Start &lt;= now &lt; End</c>) must stay with the same start and an end
    /// that is not before now. Every other block of the new list must start at or after now. A block that ends exactly
    /// at now counts as ended, and a block that starts exactly at now counts as running.
    /// </summary>
    /// <param name="before">The blocks of today before the change.</param>
    /// <param name="after">The blocks of today after the change.</param>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <returns><see langword="true"/> when the change keeps the past part of today.</returns>
    public static bool AreUnchanged(
        IReadOnlyCollection<DayBlock> before,
        IReadOnlyCollection<DayBlock> after,
        DateTime nowUtc
    )
    {
        var isPastKept = before.All(block => IsKept(block, after, nowUtc));
        var isNewStartLate = after.All(block => block.StartUtc >= nowUtc || IsPastBlock(block, before, nowUtc));

        return isPastKept && isNewStartLate;
    }

    private static bool IsKept(DayBlock block, IEnumerable<DayBlock> after, DateTime nowUtc)
    {
        if (block.EndUtc <= nowUtc)
        {
            return after.Any(next => next.StartUtc == block.StartUtc && next.EndUtc == block.EndUtc);
        }

        if (block.StartUtc <= nowUtc)
        {
            return after.Any(next => next.StartUtc == block.StartUtc && next.EndUtc >= nowUtc);
        }

        return true;
    }

    private static bool IsPastBlock(DayBlock block, IEnumerable<DayBlock> before, DateTime nowUtc) =>
        before.Any(previous => previous.StartUtc == block.StartUtc
            && (previous.EndUtc <= nowUtc ? previous.EndUtc == block.EndUtc : nowUtc < previous.EndUtc));
}
