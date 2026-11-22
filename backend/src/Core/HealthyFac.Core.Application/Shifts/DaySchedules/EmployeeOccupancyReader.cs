using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts.DaySchedules;

/// <summary>
/// Reads the occupancies of the scheduled work orders of one employee, so the schedule lookups find the free time.
/// </summary>
/// <param name="dbContext">Reads the work orders and their history.</param>
public sealed class EmployeeOccupancyReader(ICoreDbContext dbContext)
{
    /// <summary>
    /// How far before a range a work order may start and still occupy the range: the longest estimate plus a late
    /// completion.
    /// </summary>
    private const int OccupancyLookbackHours = 36;

    private readonly ICoreDbContext _dbContext = dbContext;

    /// <summary>
    /// Reads the occupancies that can touch a range of instants. A completed work order occupies up to its completion
    /// moment when it finished after its planned end.
    /// </summary>
    /// <param name="companyId">The company of the employee.</param>
    /// <param name="employeeId">The employee.</param>
    /// <param name="fromUtc">The start of the range.</param>
    /// <param name="toUtc">The end of the range.</param>
    /// <param name="excludeWorkOrderId">A work order whose slot does not count, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The occupancies of the scheduled work orders.</returns>
    public async Task<IReadOnlyList<Occupancy>> ReadAsync(
        Guid companyId,
        Guid employeeId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? excludeWorkOrderId,
        CancellationToken cancellationToken
    )
    {
        var lookbackUtc = fromUtc.AddHours(-OccupancyLookbackHours);

        var rows = await (from row in _dbContext.WorkOrderHistories.AsNoTracking()
                join workOrder in _dbContext.WorkOrders.AsNoTracking() on row.WorkOrderId equals workOrder.Id
                where workOrder.CompanyId == companyId
                    && row.Revision == workOrder.CurrentRevision
                    && row.AssigneeId == employeeId
                    && row.Status != WorkOrderStatus.Backlog
                    && row.PlannedStartUtc >= lookbackUtc
                    && row.PlannedStartUtc < toUtc
                    && workOrder.Id != excludeWorkOrderId
                orderby row.PlannedStartUtc, workOrder.Id
                select new
                {
                    workOrder.Id,
                    workOrder.Estimate,
                    row.Status,
                    row.PlannedStartUtc,
                    ChangedAt = row.CreatedAt,
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => Occupancy.Of(
                row.Id,
                row.PlannedStartUtc!.Value,
                row.Estimate,
                row.Status == WorkOrderStatus.Completed ? row.ChangedAt : null))
            .ToList();
    }
}
