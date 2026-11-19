namespace HealthyFac.Insights.Application.WorkOrders;

/// <summary>
/// Reads work orders of Core. The reader holds no assignee and no employee name.
/// </summary>
public interface IWorkOrderCatalog
{
    /// <summary>
    /// Lists the open work orders of a machine, newest first.
    /// </summary>
    /// <param name="machineId">The machine of the work orders.</param>
    /// <param name="limit">The most work orders to return.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open work orders.</returns>
    Task<IReadOnlyList<OpenWorkOrderInfo>> ListOpenAsync(
        Guid machineId,
        int limit,
        CancellationToken cancellationToken
    );
}

public sealed record OpenWorkOrderInfo
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    public DateTime? PlannedEndUtc { get; init; }
}
