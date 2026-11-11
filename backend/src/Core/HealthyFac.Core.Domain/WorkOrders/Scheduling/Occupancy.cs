namespace HealthyFac.Core.Domain.WorkOrders.Scheduling;

/// <summary>
/// The time interval that a scheduled work order blocks for its assignee, as UTC instants. The interval is half-open:
/// it holds <see cref="StartUtc"/> and ends before <see cref="EndUtc"/>, so another work order may start exactly at
/// <see cref="EndUtc"/>.
/// </summary>
public sealed class Occupancy
{
    public required Guid WorkOrderId { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    /// <summary>
    /// Builds the occupancy of a scheduled work order: from the planned start to the later of the planned end and the
    /// completion moment. A completed work order that finished late blocks the time that it really used.
    /// </summary>
    /// <param name="workOrderId">The work order.</param>
    /// <param name="plannedStartUtc">The planned start as a UTC instant.</param>
    /// <param name="estimate">The planned working time.</param>
    /// <param name="completedAtUtc">
    /// The moment of the completion while the current status is completed, else <see langword="null"/>.
    /// </param>
    /// <returns>The occupancy.</returns>
    public static Occupancy Of(
        Guid workOrderId,
        DateTime plannedStartUtc,
        TimeSpan estimate,
        DateTime? completedAtUtc
    )
    {
        var plannedEndUtc = plannedStartUtc + estimate;

        return new Occupancy
        {
            WorkOrderId = workOrderId,
            StartUtc = plannedStartUtc,
            EndUtc = completedAtUtc is { } completedAt && completedAt > plannedEndUtc ? completedAt : plannedEndUtc,
        };
    }
}
