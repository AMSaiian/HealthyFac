using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// Tells where the blocks of a day come from. The camelCase values reach the API, so never rename one in place.
/// </summary>
public sealed class DayScheduleSource : Enumeration<DayScheduleSource>
{
    public static readonly DayScheduleSource Template = new(value: "template");
    public static readonly DayScheduleSource Override = new(value: "override");
    public static readonly DayScheduleSource None = new(value: "none");

    private DayScheduleSource(string value)
        : base(value)
    {
    }
}
