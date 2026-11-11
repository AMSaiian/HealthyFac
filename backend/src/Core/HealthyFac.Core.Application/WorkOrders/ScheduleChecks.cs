using FluentValidation.Results;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// The four rules for a slot of a work order. Create, assign, move, and update use them in one serializable
/// transaction: the assignee, the planned start, the work block, and the overlap with other work orders.
/// </summary>
/// <param name="dbContext">Reads the employee and the other work orders.</param>
/// <param name="reader">Loads the shifts of the assignee.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class ScheduleChecks(ICoreDbContext dbContext, DayScheduleReader reader, TimeProvider timeProvider)
{
    private const string PlannedStartKey = "plannedStart";
    private const int OccupancyLookbackHours = 36;

    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Checks the slot of a work order for an assignee and returns the planned start as a UTC instant. Rule 1: the
    /// assignee is an active employee of the company. Rule 2: the local start is on the grid, exists in the zone, is
    /// not in the past, and is at most 365 days ahead. Rule 3: the slot fits one block of the day. Rule 4: the slot
    /// overlaps no other scheduled work order of the assignee.
    /// </summary>
    /// <param name="companyId">The company.</param>
    /// <param name="assigneeId">The assignee.</param>
    /// <param name="plannedStartLocal">The planned start as a time in the company zone.</param>
    /// <param name="estimate">The planned working time.</param>
    /// <param name="workOrderId">The work order that the slot is for, so its own old slot does not count.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The planned start as a UTC instant.</returns>
    /// <exception cref="NotFoundException">The assignee is not an employee of the company.</exception>
    /// <exception cref="UnprocessableException">
    /// The assignee is not active, the slot is outside the shift, or it overlaps another work order.
    /// </exception>
    /// <exception cref="AppValidationException">The planned start breaks rule 2.</exception>
    public async Task<DateTime> EnsureAsync(
        Guid companyId,
        Guid assigneeId,
        DateTime plannedStartLocal,
        TimeSpan estimate,
        Guid? workOrderId,
        CancellationToken cancellationToken
    )
    {
        await EnsureAssigneeAsync(companyId, assigneeId, cancellationToken);

        var zone = await _reader.GetZoneAsync(companyId, cancellationToken);
        var startUtc = EnsureStart(plannedStartLocal, zone);

        await EnsureSlotAsync(
            companyId, assigneeId, plannedStartLocal, startUtc, estimate, workOrderId, cancellationToken);

        return startUtc;
    }

    /// <summary>
    /// Checks rules 3 and 4 for a slot that already has its planned start: the slot fits one block of the day, and it
    /// overlaps no other scheduled work order of the assignee. A change of the estimate of a scheduled work order uses
    /// it, because the start and the assignee already passed rules 1 and 2.
    /// </summary>
    /// <param name="companyId">The company.</param>
    /// <param name="assigneeId">The assignee.</param>
    /// <param name="plannedStartLocal">The planned start as a time in the company zone.</param>
    /// <param name="plannedStartUtc">The same start as a UTC instant.</param>
    /// <param name="estimate">The planned working time.</param>
    /// <param name="workOrderId">The work order that the slot is for, so its own old slot does not count.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>A task that completes when the slot is allowed.</returns>
    /// <exception cref="UnprocessableException">
    /// The slot is outside the shift or overlaps another work order.
    /// </exception>
    public async Task EnsureSlotAsync(
        Guid companyId,
        Guid assigneeId,
        DateTime plannedStartLocal,
        DateTime plannedStartUtc,
        TimeSpan estimate,
        Guid? workOrderId,
        CancellationToken cancellationToken
    )
    {
        var zone = await _reader.GetZoneAsync(companyId, cancellationToken);
        var date = DateOnly.FromDateTime(plannedStartLocal);
        var shifts = await _reader.LoadAsync(assigneeId, zone, date, date, isTracked: false, cancellationToken);
        var slot = Occupancy.Of(workOrderId ?? Guid.Empty, plannedStartUtc, estimate, completedAtUtc: null);
        if (!SlotRules.FitsOneBlock(slot, shifts.Resolve(date, date).Single()))
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.OutsideShift);
        }

        var occupied = await ReadOccupanciesAsync(companyId, assigneeId, slot, cancellationToken);
        if (SlotRules.Overlaps(slot, occupied))
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.SlotTaken);
        }
    }

    private async Task EnsureAssigneeAsync(Guid companyId, Guid assigneeId, CancellationToken cancellationToken)
    {
        var status = await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == assigneeId && employee.CompanyId == companyId)
            .Select(employee => employee.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.AssigneeNotFound);
        }

        if (status != EmployeeStatus.Active)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.AssigneeNotActive);
        }
    }

    private DateTime EnsureStart(DateTime plannedStartLocal, TimeZoneInfo zone)
    {
        if (plannedStartLocal.TimeOfDay.Ticks % SlotRules.GridStep.Ticks != 0)
        {
            throw Invalid(WorkOrderErrors.Validation.StartOffGrid);
        }

        if (LocalTimes.IsSkipped(plannedStartLocal, zone))
        {
            throw Invalid(ShiftErrors.Validation.LocalTimeDoesNotExist);
        }

        var startUtc = LocalTimes.ToUtc(plannedStartLocal, zone);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        var lastDate = today.AddDays(ShiftValidationConstants.MaxDaysAhead);
        if (startUtc < nowUtc || DateOnly.FromDateTime(plannedStartLocal) > lastDate)
        {
            throw Invalid(WorkOrderErrors.Validation.StartOutOfRange);
        }

        return startUtc;
    }

    private static AppValidationException Invalid(string message) =>
        new([new ValidationFailure(PlannedStartKey, message)]);

    private async Task<List<Occupancy>> ReadOccupanciesAsync(
        Guid companyId,
        Guid assigneeId,
        Occupancy slot,
        CancellationToken cancellationToken
    )
    {
        var fromUtc = slot.StartUtc.AddHours(-OccupancyLookbackHours);

        var rows = await (from row in _dbContext.WorkOrderHistories.AsNoTracking()
                join workOrder in _dbContext.WorkOrders.AsNoTracking() on row.WorkOrderId equals workOrder.Id
                where workOrder.CompanyId == companyId
                    && row.Revision == workOrder.CurrentRevision
                    && row.AssigneeId == assigneeId
                    && row.Status != WorkOrderStatus.Backlog
                    && row.PlannedStartUtc >= fromUtc
                    && row.PlannedStartUtc < slot.EndUtc
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
