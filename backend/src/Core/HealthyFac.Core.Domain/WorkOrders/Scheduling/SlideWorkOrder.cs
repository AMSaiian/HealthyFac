namespace HealthyFac.Core.Domain.WorkOrders.Scheduling;

/// <summary>
/// A scheduled work order as the completion slide sees it.
/// </summary>
public sealed class SlideWorkOrder
{
    public required Guid Id { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public required DateTime PlannedStartUtc { get; init; }

    public required TimeSpan Estimate { get; init; }

    /// <summary>The completion moment while the status is completed, else <see langword="null"/>.</summary>
    public DateTime? CompletedAtUtc { get; init; }
}
