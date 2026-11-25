using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HealthyFac.Core.Contracts.WorkOrders;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

public static class CoreWorkOrderCatalogTestData
{
    public const string CreatedAtColumn = "createdAt";
    public const string DescendingDirection = "desc";
    public const string DownstreamScope = "work-orders";
    public const int Limit = 7;

    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c01");
    public static readonly Guid WorkOrderId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c02");

    public static readonly DateTime PlannedStart = new(
        year: 2027, month: 3, day: 8, hour: 9, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly string[] OpenStatuses = ["backlog", "staged", "inProgress", "blocked"];

    public static TheoryData<string, StatusCode> OutageStatuses => new()
    {
        { "Unavailable", StatusCode.Unavailable },
        { "DeadlineExceeded", StatusCode.DeadlineExceeded },
    };

    public static SearchWorkOrdersReply CreateReply(bool hasPlannedTimes)
    {
        var row = new WorkOrderRow
        {
            Id = WorkOrderId.ToString(),
            Title = "Replace the filter",
            Status = "staged",
            Priority = "high",
            Category = "repair",
            EstimateMinutes = 60,
        };

        if (hasPlannedTimes)
        {
            row.PlannedStart = Timestamp.FromDateTime(PlannedStart);
            row.PlannedEnd = Timestamp.FromDateTime(PlannedStart.AddHours(value: 1));
        }

        row.AssigneeId = Guid.CreateVersion7().ToString();
        row.AssigneeName = "Wanda Worker";

        return new SearchWorkOrdersReply { WorkOrders = { row }, TotalCount = 1 };
    }
}
