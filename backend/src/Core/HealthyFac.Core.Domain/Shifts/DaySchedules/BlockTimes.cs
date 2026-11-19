namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// The local start and end of one work block of a day. The end is after the start. Both are times in the company zone.
/// </summary>
public readonly record struct BlockTimes
{
    public required TimeOnly Start { get; init; }

    public required TimeOnly End { get; init; }
}
