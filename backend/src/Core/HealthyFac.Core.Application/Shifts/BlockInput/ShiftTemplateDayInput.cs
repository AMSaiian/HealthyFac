using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.Shifts.DaySchedules;

namespace HealthyFac.Core.Application.Shifts.BlockInput;

/// <summary>
/// The blocks of one weekday of a template version as a client sends them.
/// </summary>
public record ShiftTemplateDayInput
{
    public required Weekday Weekday { get; init; }

    public required IReadOnlyList<ShiftBlockInput> Blocks { get; init; }

    /// <summary>
    /// Flattens the days into the weekday and times pairs that the domain takes.
    /// </summary>
    /// <param name="days">The days of the request.</param>
    /// <returns>One pair for each block.</returns>
    public static IReadOnlyList<(Weekday Weekday, BlockTimes Times)> ToTemplateBlocks(
        IEnumerable<ShiftTemplateDayInput> days
    ) =>
        days.SelectMany(day => day.Blocks.Select(block => (day.Weekday, block.ToTimes()))).ToList();
}
