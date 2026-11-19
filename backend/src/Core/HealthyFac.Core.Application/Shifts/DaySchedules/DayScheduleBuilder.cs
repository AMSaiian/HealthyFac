using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Builds the day responses of one or several employees for one local date. The employee day and the company day share
/// it, so both resolve the blocks and read the work orders the same way.
/// </summary>
/// <param name="dbContext">Reads the shifts and the work orders.</param>
/// <param name="reader">Reads the zone of the company.</param>
/// <param name="timeProvider">Provides the time that decides whether a work order is delayed.</param>
public sealed class DayScheduleBuilder(ICoreDbContext dbContext, DayScheduleReader reader, TimeProvider timeProvider)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Builds the day of each employee.
    /// </summary>
    /// <param name="companyId">The company of the employees.</param>
    /// <param name="employeeIds">The employees.</param>
    /// <param name="date">The local date of the company.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The day of each employee by employee ID.</returns>
    public async Task<IReadOnlyDictionary<Guid, DayScheduleResponse>> BuildAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> employeeIds,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        var zone = await _reader.GetZoneAsync(companyId, cancellationToken);
        var versions = await _dbContext.ShiftTemplateVersions
            .AsNoTracking()
            .Include(version => version.Blocks)
            .Where(version => employeeIds.Contains(version.EmployeeId))
            .ToListAsync(cancellationToken);
        var overrides = await _dbContext.ShiftOverrides
            .AsNoTracking()
            .Include(shiftOverride => shiftOverride.Blocks)
            .Where(shiftOverride => employeeIds.Contains(shiftOverride.EmployeeId) && shiftOverride.Date == date)
            .ToListAsync(cancellationToken);
        var workOrders = await ReadWorkOrdersAsync(companyId, employeeIds, date, zone, cancellationToken);

        return employeeIds.ToDictionary(
            employeeId => employeeId,
            employeeId => ToResponse(
                date,
                zone,
                DayScheduleResolver.Resolve(
                    date,
                    zone,
                    versions.Where(version => version.EmployeeId == employeeId),
                    overrides.SingleOrDefault(shiftOverride => shiftOverride.EmployeeId == employeeId)),
                workOrders.Where(order => order.AssigneeId == employeeId).Select(order => order.Response).ToList()));
    }

    private static DayScheduleResponse ToResponse(
        DateOnly date,
        TimeZoneInfo zone,
        DaySchedule day,
        IReadOnlyList<DayScheduleWorkOrderResponse> workOrders
    ) =>
        new()
        {
            Date = date,
            TimeZoneId = zone.Id,
            Source = day.Source,
            Blocks = day.Blocks
                .Select(block => new DayScheduleBlockResponse
                {
                    StartLocal = block.Start,
                    EndLocal = block.End,
                    StartUtc = block.StartUtc,
                    EndUtc = block.EndUtc,
                })
                .ToList(),
            WorkOrders = workOrders,
        };

    private async Task<List<AssignedWorkOrder>> ReadWorkOrdersAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> employeeIds,
        DateOnly date,
        TimeZoneInfo zone,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var dayStartUtc = LocalTimes.ToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
        var dayEndUtc = LocalTimes.ToUtc(date.AddDays(value: 1).ToDateTime(TimeOnly.MinValue), zone);

        var rows = await (from row in _dbContext.WorkOrderHistories.AsNoTracking()
                join workOrder in _dbContext.WorkOrders.AsNoTracking() on row.WorkOrderId equals workOrder.Id
                where workOrder.CompanyId == companyId
                    && row.Revision == workOrder.CurrentRevision
                    && employeeIds.Contains(row.AssigneeId!.Value)
                    && row.Status != WorkOrderStatus.Backlog
                    && row.PlannedStartUtc >= dayStartUtc
                    && row.PlannedStartUtc < dayEndUtc
                orderby row.PlannedStartUtc, workOrder.Id
                select new
                {
                    workOrder.Id,
                    workOrder.Title,
                    workOrder.Priority,
                    workOrder.Estimate,
                    workOrder.RowVersion,
                    row.AssigneeId,
                    row.Status,
                    row.PlannedStartLocal,
                    row.PlannedStartUtc,
                    ChangedAt = row.CreatedAt,
                })
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
            {
                var plannedEndUtc = row.PlannedStartUtc!.Value + row.Estimate;
                var isLateCompletion = row.Status == WorkOrderStatus.Completed && row.ChangedAt > plannedEndUtc;

                return new AssignedWorkOrder
                {
                    AssigneeId = row.AssigneeId!.Value,
                    Response = new DayScheduleWorkOrderResponse
                    {
                        Id = row.Id,
                        Title = row.Title,
                        Status = row.Status,
                        Priority = row.Priority,
                        PlannedStartLocal = row.PlannedStartLocal!.Value,
                        PlannedStartUtc = row.PlannedStartUtc.Value,
                        EstimateMinutes = (int)row.Estimate.TotalMinutes,
                        OccupancyEndUtc = isLateCompletion ? row.ChangedAt : plannedEndUtc,
                        IsDelayed = row.Status == WorkOrderStatus.Staged && row.PlannedStartUtc < nowUtc,
                        RowVersion = row.RowVersion,
                    },
                };
            })
            .ToList();
    }

    private sealed class AssignedWorkOrder
    {
        public required Guid AssigneeId { get; init; }

        public required DayScheduleWorkOrderResponse Response { get; init; }
    }
}
