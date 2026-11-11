using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.UnitTests.WorkOrders.Data;

public static class WorkOrderTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0f6c1d52-7a3e-4b8e-9d11-2f3a5c7e9b01");
    public static readonly Guid AssigneeId = Guid.Parse(input: "1a7d2e63-8b4f-4c9f-8e22-3a4b6d8f0c12");
    public static readonly Guid OtherAssigneeId = Guid.Parse(input: "2b8e3f74-9c50-4da0-9f33-4b5c7e9a1d23");
    public static readonly DateTime StartLocal = new(year: 2027, month: 3, day: 29, hour: 9, minute: 0, second: 0);
    public static readonly DateTime StartUtc = new(year: 2027, month: 3, day: 29, hour: 6, minute: 0, second: 0);

    public static readonly DateTime LaterStartLocal = new(
        year: 2027, month: 3, day: 29, hour: 14, minute: 0, second: 0);

    public static readonly DateTime LaterStartUtc = new(year: 2027, month: 3, day: 29, hour: 11, minute: 0, second: 0);
    public static readonly Guid FileA = Guid.Parse(input: "3c9f4085-ad61-4eb1-a044-5c6d8fab2e34");
    public static readonly Guid FileB = Guid.Parse(input: "4da05196-be72-4fc2-b155-6d7e9abc3f45");
    public static readonly Guid FileC = Guid.Parse(input: "5eb162a7-cf83-40d3-8266-7e8fabcd4056");

    public static WorkOrder CreateBacklog() =>
        WorkOrder.Create(
            CompanyId,
            title: "Replace the filter",
            description: null,
            WorkOrderPriority.Medium,
            WorkOrderCategory.Repair,
            machineId: null,
            TimeSpan.FromHours(value: 1));

    public static WorkOrder CreateStaged()
    {
        var workOrder = CreateBacklog();
        workOrder.Assign(AssigneeId, StartLocal, StartUtc);

        return workOrder;
    }

    public static WorkOrder CreateInProgress()
    {
        var workOrder = CreateStaged();
        workOrder.ChangeStatus(WorkOrderStatus.InProgress);

        return workOrder;
    }
}
