namespace HealthyFac.Core.Domain.WorkOrders.Scheduling;

/// <summary>
/// One change that the completion slide makes: a move to a new start, or a move to the backlog.
/// </summary>
public sealed class SlideMove
{
    public required Guid WorkOrderId { get; init; }

    /// <summary>
    /// The new planned start as a UTC instant, or <see langword="null"/> to send the work order to the backlog.
    /// </summary>
    public DateTime? NewStartUtc { get; init; }
}
