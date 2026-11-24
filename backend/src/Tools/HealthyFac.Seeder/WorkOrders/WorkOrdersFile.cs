namespace HealthyFac.Seeder.WorkOrders;

/// <summary>
/// The content of <c>work-orders.json</c>: the work orders with their assignments and status changes.
/// </summary>
public sealed class WorkOrdersFile
{
    public required IReadOnlyList<WorkOrderSeed> WorkOrders { get; init; }
}
