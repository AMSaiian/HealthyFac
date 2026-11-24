using HealthyFac.Core.Domain.Shifts.DaySchedules;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// Joins blocks that touch, so one work period is one block: the end of a block is the start of the next one.
/// </summary>
public static class BlockJoin
{
    /// <summary>
    /// Sorts the blocks by start and joins each block whose start equals the end of the block before it. A gap keeps
    /// two blocks.
    /// </summary>
    /// <param name="blocks">The blocks of one day.</param>
    /// <returns>The joined blocks in start order.</returns>
    public static List<BlockTimes> Join(IEnumerable<BlockTimes> blocks) =>
        JoinRanges(blocks.Select(block => (block.Start, block.End)))
            .Select(range => new BlockTimes { Start = range.Start, End = range.End })
            .ToList();

    /// <summary>
    /// The one join rule for ranges of any point type (local times or UTC instants): sort by start, then join a range
    /// whose start equals the end of the range before it.
    /// </summary>
    /// <param name="ranges">The ranges to join.</param>
    /// <typeparam name="T">The type of a range point.</typeparam>
    /// <returns>The joined ranges in start order.</returns>
    public static List<(T Start, T End)> JoinRanges<T>(IEnumerable<(T Start, T End)> ranges)
        where T : struct, IComparable<T>
    {
        var joined = new List<(T Start, T End)>();
        foreach (var range in ranges.OrderBy(candidate => candidate.Start))
        {
            if (joined.Count > 0 && joined[^1].End.CompareTo(range.Start) == 0)
            {
                joined[^1] = (joined[^1].Start, range.End);
            }
            else
            {
                joined.Add(range);
            }
        }

        return joined;
    }

    /// <summary>
    /// Joins the blocks of each weekday of a template version.
    /// </summary>
    /// <param name="blocks">The blocks of the version with their weekdays.</param>
    /// <returns>The joined blocks, grouped by weekday in weekday order.</returns>
    public static List<(Weekday Weekday, BlockTimes Times)> JoinPerWeekday(
        IEnumerable<(Weekday Weekday, BlockTimes Times)> blocks
    ) =>
        blocks.GroupBy(block => block.Weekday)
            .OrderBy(group => group.Key)
            .SelectMany(group => Join(group.Select(block => block.Times)).Select(times => (group.Key, times)))
            .ToList();
}
