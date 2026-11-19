using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// One work block of a day as a client sends it, in times of the company zone.
/// </summary>
public record ShiftBlockInput
{
    public required TimeOnly Start { get; init; }

    public required TimeOnly End { get; init; }

    public BlockTimes ToTimes() => new() { Start = Start, End = End };
}
