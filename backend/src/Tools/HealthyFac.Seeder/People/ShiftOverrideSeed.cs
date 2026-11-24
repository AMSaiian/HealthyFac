using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.People;

/// <summary>The blocks of one day of one person. An empty list is a day off.</summary>
public sealed class ShiftOverrideSeed
{
    public required string Person { get; init; }

    /// <summary>The day. Only the date of the moment counts; the time of day is ignored.</summary>
    public required RelativeMoment Day { get; init; }

    public required IReadOnlyList<BlockSeed> Blocks { get; init; }

    public required string SetBy { get; init; }

    public required RelativeMoment SetAt { get; init; }
}
