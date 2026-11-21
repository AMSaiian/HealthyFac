using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// The result of a status change, for its real-time events. A use case returns it from its transaction operation,
/// with the identity users of the slide moves read inside the operation.
/// </summary>
public sealed record WorkOrderStatusChange
{
    public required Guid WorkOrderId { get; init; }

    public required string Title { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public required IReadOnlyList<WorkOrderSlideMove> SlideMoves { get; init; }
}
