using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.WorkOrders.ApplySchedule;

/// <summary>
/// Names why one change of an apply is rejected. The value travels in the problem details, so treat it as a contract.
/// </summary>
public sealed class ScheduleChangeRejection : Enumeration<ScheduleChangeRejection>
{
    public static readonly ScheduleChangeRejection NotEditable = new(value: "notEditable");
    public static readonly ScheduleChangeRejection AssigneeNotActive = new(value: "assigneeNotActive");
    public static readonly ScheduleChangeRejection StartOutOfRange = new(value: "startOutOfRange");
    public static readonly ScheduleChangeRejection OutsideShift = new(value: "outsideShift");
    public static readonly ScheduleChangeRejection SlotTaken = new(value: "slotTaken");

    private ScheduleChangeRejection(string value)
        : base(value)
    {
    }
}
