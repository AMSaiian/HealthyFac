namespace HealthyFac.Core.Domain.Shifts.DaySchedules;

/// <summary>
/// Merges the override of today with the template of today when the override is deleted.
/// </summary>
public static class TodayOverrideMerge
{
    /// <summary>
    /// Keeps the past part of today and returns the rest to the template. Before <paramref name="nowGridUtc"/>, every
    /// override block is cut to end at most at that instant. From <paramref name="nowGridUtc"/>, every template block
    /// is cut to start at least at that instant. One join exists: the part of an override block that was running at
    /// <paramref name="nowUtc"/> (its end is after now) joins the template part that starts at
    /// <paramref name="nowGridUtc"/>. Every other pair stays separate: touching override blocks, touching template
    /// blocks, and an override block that ended at or before now.
    /// </summary>
    /// <param name="overrideBlocks">The blocks of the override day.</param>
    /// <param name="templateBlocks">The blocks of the template day, without the override.</param>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <param name="nowGridUtc">
    /// Now rounded up to the grid of <see cref="WorkOrders.Scheduling.SlotRules.GridStep"/>.
    /// </param>
    /// <returns>The merged blocks, sorted by start.</returns>
    public static IReadOnlyList<DayBlock> Merge(
        IReadOnlyCollection<DayBlock> overrideBlocks,
        IReadOnlyCollection<DayBlock> templateBlocks,
        DateTime nowUtc,
        DateTime nowGridUtc
    )
    {
        var overrideParts = overrideBlocks
            .Select(block => (Block: block, Part: Cut(block, block.StartUtc, Min(block.EndUtc, nowGridUtc))))
            .Where(candidate => candidate.Part is not null)
            .ToList();

        var templateParts = templateBlocks
            .Select(block => Cut(block, Max(block.StartUtc, nowGridUtc), block.EndUtc))
            .OfType<DayBlock>()
            .ToList();

        var (_, part) = overrideParts.Find(candidate =>
            candidate.Block.EndUtc > nowUtc && candidate.Part!.EndUtc == nowGridUtc);

        var templateStart = templateParts.Find(part => part.StartUtc == nowGridUtc);
        var parts = overrideParts.Select(candidate => candidate.Part!).ToList();

        if (part is not null && templateStart is not null)
        {
            parts.Remove(part);
            templateParts.Remove(templateStart);

            parts.Add(
                new DayBlock
                {
                    Start = part.Start,
                    End = templateStart.End,
                    StartUtc = part.StartUtc,
                    EndUtc = templateStart.EndUtc,
                });
        }

        return [.. parts.Concat(templateParts).OrderBy(part => part.StartUtc)];
    }

    private static DayBlock? Cut(DayBlock block, DateTime startUtc, DateTime endUtc) =>
        startUtc >= endUtc
            ? null
            : new DayBlock
            {
                Start = block.Start.Add(startUtc - block.StartUtc),
                End = block.End.Add(endUtc - block.EndUtc),
                StartUtc = startUtc,
                EndUtc = endUtc,
            };

    private static DateTime Min(DateTime first, DateTime second) => first < second ? first : second;

    private static DateTime Max(DateTime first, DateTime second) => first > second ? first : second;
}
