namespace HealthyFac.Core.Domain.Shifts.DaySchedules;

/// <summary>
/// Protects the part of today that already happened: the ended blocks, and the start of a running block.
/// </summary>
public static class PastBlocks
{
    /// <summary>
    /// Compares the blocks of today before and after a change, after both lists join their touching blocks. A block of
    /// the old list that starts at or before now needs a block of the new list with the same start and the same end
    /// cut at now (<c>min(End, now)</c>): an ended block stays as it is, and a running block keeps its start and may
    /// end at now or later. A block of the new list that starts before now needs such an old block. Every other new
    /// block starts at or after now. So a block that ends exactly at now may grow when a new block touches it.
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
        var oldBlocks = JoinTouching(before);
        var newBlocks = JoinTouching(after);
        var isPastKept = oldBlocks.Where(block => block.Start <= nowUtc)
            .All(block => HasPastTwin(block, newBlocks, nowUtc));
        var isNewStartLate = newBlocks.Where(block => block.Start < nowUtc)
            .All(block => HasPastTwin(block, oldBlocks, nowUtc));

        return isPastKept && isNewStartLate;
    }

    private static List<(DateTime Start, DateTime End)> JoinTouching(IEnumerable<DayBlock> blocks) =>
        BlockJoin.JoinRanges(blocks.Select(block => (block.StartUtc, block.EndUtc)));

    private static bool HasPastTwin(
        (DateTime Start, DateTime End) block,
        IEnumerable<(DateTime Start, DateTime End)> others,
        DateTime nowUtc
    ) =>
        others.Any(other => other.Start == block.Start && Min(other.End, nowUtc) == Min(block.End, nowUtc));

    private static DateTime Min(DateTime first, DateTime second) => first <= second ? first : second;
}
