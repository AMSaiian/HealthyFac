namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// The work blocks of one employee on one local date of the company, sorted by start.
/// </summary>
public sealed class DaySchedule
{
    public required DateOnly Date { get; init; }

    public required DayScheduleSource Source { get; init; }

    public required IReadOnlyList<DayBlock> Blocks { get; init; }
}
