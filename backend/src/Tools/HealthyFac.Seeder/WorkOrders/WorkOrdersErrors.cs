namespace HealthyFac.Seeder.WorkOrders;

/// <summary>Defines the error messages of the work orders part.</summary>
public static class WorkOrdersErrors
{
    public static string PersonUnknown(string item, string person) =>
        $"The event {item} names the person {person}, who is not in company.json.";

    public static string PlannedStartNeedsTime(string item) =>
        $"The event {item} needs a planned start with a time of day.";

    public static string StatusNeedsAssignment(string item) =>
        $"The event {item} changes the status of a work order that has no assignment.";

    public static string OrderMissing(Guid orderId) => $"Core holds no work order {orderId}.";
}
