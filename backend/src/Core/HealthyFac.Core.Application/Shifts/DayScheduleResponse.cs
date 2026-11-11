using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// The day of one employee: the blocks with their local times and UTC instants, where they come from, and the
/// scheduled work orders that start on the date.
/// </summary>
public record DayScheduleResponse
{
    public required DateOnly Date { get; init; }

    /// <summary>The IANA zone of the company. The local times of the response are times in this zone.</summary>
    public required string TimeZoneId { get; init; }

    public required DayScheduleSource Source { get; init; }

    public required IReadOnlyList<DayScheduleBlockResponse> Blocks { get; init; }

    public required IReadOnlyList<DayScheduleWorkOrderResponse> WorkOrders { get; init; }
}

public record DayScheduleBlockResponse
{
    public required TimeOnly StartLocal { get; init; }

    public required TimeOnly EndLocal { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }
}

public record DayScheduleWorkOrderResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required DateTime PlannedStartLocal { get; init; }

    public required DateTime PlannedStartUtc { get; init; }

    public required int EstimateMinutes { get; init; }

    /// <summary>
    /// The end of the time that the work order blocks: the planned end, or the completion moment when a completed work
    /// order finished later.
    /// </summary>
    public required DateTime OccupancyEndUtc { get; init; }

    public required bool IsDelayed { get; init; }

    public required uint RowVersion { get; init; }
}
