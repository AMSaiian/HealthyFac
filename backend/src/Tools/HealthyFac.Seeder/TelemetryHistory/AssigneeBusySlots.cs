using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.WorkOrders;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// Builds the slots that the work orders of <c>work-orders.json</c> take from one assignee. The job knows them from
/// the file before the events run, so a drain order never takes a slot of an order that is not stored yet.
/// </summary>
internal static class AssigneeBusySlots
{
    /// <summary>Builds the slot of each assigned order of the assignee.</summary>
    /// <param name="orders">The work orders of the file.</param>
    /// <param name="assignee">The key of the assignee.</param>
    /// <param name="anchor">The start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <returns>The slots: from the planned start to the later of the planned end and the completion.</returns>
    public static IReadOnlyList<Occupancy> FromFile(
        IEnumerable<WorkOrderSeed> orders,
        string assignee,
        DateTimeOffset anchor,
        TimeZoneInfo zone
    ) =>
    [
        .. orders.Where(order => order.Assignment?.Assignee == assignee)
            .Select(order =>
            {
                var completion = order.StatusChanges.LastOrDefault();

                return Occupancy.Of(
                    Guid.NewGuid(),
                    SeedCalendar.ToUtc(order.Assignment!.PlannedStart, anchor, zone).UtcDateTime,
                    TimeSpan.FromMinutes(order.EstimateMinutes),
                    completion?.Status == WorkOrderStatus.Completed
                        ? SeedCalendar.ToUtc(completion.At, anchor, zone).UtcDateTime
                        : null);
            }),
    ];
}
