using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Rejects a shift change that breaks the past part of today or leaves a future scheduled work order outside the
/// blocks. Nothing moves automatically: the caller changes nothing when the guard throws.
/// </summary>
/// <param name="dbContext">Reads the scheduled work orders of the employee.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class ShiftChangeGuard(ICoreDbContext dbContext, TimeProvider timeProvider)
{
    public const string WorkOrdersKey = "workOrders";

    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Compares the day schedules before and after a change. For today, the ended blocks and the start of a running
    /// block must stay. Then every scheduled work order of the employee that starts at or after now, and is not
    /// completed, must still fit one block of its day.
    /// </summary>
    /// <param name="companyId">The company of the employee.</param>
    /// <param name="employeeId">The employee whose shifts change.</param>
    /// <param name="zone">The zone of the company.</param>
    /// <param name="before">The resolved days before the change.</param>
    /// <param name="after">The resolved days after the change, for the same dates.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>A task that completes when the change is allowed.</returns>
    /// <exception cref="UnprocessableException">
    /// The change breaks the past part of today, or leaves work orders outside the shift. The second case lists at
    /// most 50 of them under <see cref="WorkOrdersKey"/>, by planned start.
    /// </exception>
    public async Task EnsureAsync(
        Guid companyId,
        Guid employeeId,
        TimeZoneInfo zone,
        IReadOnlyList<DaySchedule> before,
        IReadOnlyList<DaySchedule> after,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        var todayBefore = before.SingleOrDefault(day => day.Date == today);
        var todayAfter = after.SingleOrDefault(day => day.Date == today);
        if (todayBefore is not null
            && todayAfter is not null
            && !PastBlocks.AreUnchanged(todayBefore.Blocks.ToList(), todayAfter.Blocks.ToList(), nowUtc))
        {
            throw new UnprocessableException(ShiftErrors.Rejections.PastBlockChanged);
        }

        if (after.Count == 0)
        {
            return;
        }

        var affected = await FindAffectedAsync(companyId, employeeId, after, nowUtc, cancellationToken);
        if (affected.Count > 0)
        {
            throw new UnprocessableException(ShiftErrors.Rejections.WorkOrdersOutsideShift)
            {
                Extensions = new Dictionary<string, object?> { [WorkOrdersKey] = affected },
            };
        }
    }

    private async Task<IReadOnlyList<AffectedWorkOrder>> FindAffectedAsync(
        Guid companyId,
        Guid employeeId,
        IReadOnlyList<DaySchedule> after,
        DateTime nowUtc,
        CancellationToken cancellationToken
    )
    {
        var firstDate = after.Min(day => day.Date);
        var lastDate = after.Max(day => day.Date);

        var scheduled = await (from row in _dbContext.WorkOrderHistories.AsNoTracking()
                join workOrder in _dbContext.WorkOrders.AsNoTracking() on row.WorkOrderId equals workOrder.Id
                where workOrder.CompanyId == companyId
                    && row.Revision == workOrder.CurrentRevision
                    && row.AssigneeId == employeeId
                    && row.Status != WorkOrderStatus.Backlog
                    && row.Status != WorkOrderStatus.Completed
                    && row.PlannedStartUtc >= nowUtc
                select new
                {
                    workOrder.Id,
                    workOrder.Title,
                    workOrder.Estimate,
                    row.PlannedStartLocal,
                    row.PlannedStartUtc,
                })
            .ToListAsync(cancellationToken);

        return scheduled
            .Select(order => (Order: order, Date: DateOnly.FromDateTime(order.PlannedStartLocal!.Value)))
            .Where(candidate => candidate.Date >= firstDate && candidate.Date <= lastDate)
            .Where(candidate => !SlotRules.FitsOneBlock(
                Occupancy.Of(
                    candidate.Order.Id,
                    candidate.Order.PlannedStartUtc!.Value,
                    candidate.Order.Estimate,
                    completedAtUtc: null),
                after.Single(day => day.Date == candidate.Date)))
            .OrderBy(candidate => candidate.Order.PlannedStartUtc)
            .Take(ShiftValidationConstants.MaxAffectedWorkOrders)
            .Select(candidate => new AffectedWorkOrder
            {
                Id = candidate.Order.Id,
                Title = candidate.Order.Title,
                PlannedStartLocal = candidate.Order.PlannedStartLocal!.Value,
            })
            .ToList();
    }
}
