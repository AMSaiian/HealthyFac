using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

/// <summary>
/// Reads one page of the work orders that the access scope reaches. The single search and the batch search run this
/// query, so the scope rule has one definition. The query always holds the scope, whatever the filter says. The
/// current history row gives the status, the assignee, and the planned start. A work order is delayed when it is
/// staged and its planned start is before now; the database computes it, so the flag filters and shows.
/// </summary>
/// <param name="dbContext">Reads the work orders.</param>
/// <param name="currentUser">Reads the actor.</param>
/// <param name="timeProvider">Provides the time that decides whether a work order is delayed.</param>
public sealed class WorkOrderSearchQuery(
    ICoreDbContext dbContext,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider
)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>Reads the page of one search.</summary>
    /// <param name="search">The paging, the sort, and the filter of the search.</param>
    /// <param name="scope">The access scope of the actor.</param>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="ownEmployeeId">The employee of the actor, needed for the own scope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The page and the number of all matching rows.</returns>
    public async Task<GridResponse<WorkOrderRowResponse>> SearchAsync(
        GridRequest<WorkOrderGridFilter, WorkOrderSortColumn> search,
        AccessScope scope,
        Guid companyId,
        Guid? ownEmployeeId,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        string[] deletedFilters = [SoftDeleteConstants.QueryFilterName];

        var query = _dbContext.WorkOrders
            .AsNoTracking()
            .ApplyScope(scope, companyId, ownEmployeeId, _currentUser.GetUserId())
            .ApplyFilter(search.Filter);
        if (search.Filter.IsDelayed is { } isDelayed)
        {
            query = query.Where(
                isDelayed ? WorkOrderCurrentRow.IsDelayed(nowUtc) : WorkOrderCurrentRow.IsNotDelayed(nowUtc));
        }

        var sorted = search.Sort?.Column == WorkOrderSortColumn.IsDelayed
            ? SortByDelayed(query, search.Sort.Direction, nowUtc)
            : query.ApplySort(search.Sort, WorkOrderSortMap.Instance);

        return await sorted
            .ToGridResponseAsync(
                search.Offset,
                search.Limit,
                WorkOrderCurrentRow.Of((workOrder, row) => new WorkOrderRowResponse
                {
                    Id = workOrder.Id,
                    Title = workOrder.Title,
                    Status = row.Status,
                    Priority = workOrder.Priority,
                    Category = workOrder.Category,
                    AssigneeId = row.AssigneeId,
                    AssigneeName = _dbContext.Employees
                        .Where(employee => employee.Id == row.AssigneeId)
                        .Select(employee => employee.FirstName + " " + employee.LastName)
                        .FirstOrDefault(),
                    MachineId = workOrder.MachineId,
                    MachineName = _dbContext.Machines
                        .IgnoreQueryFilters(deletedFilters)
                        .Where(machine => machine.Id == workOrder.MachineId)
                        .Select(machine => machine.Name)
                        .FirstOrDefault(),
                    IsMachineDeleted = _dbContext.Machines
                        .IgnoreQueryFilters(deletedFilters)
                        .Any(machine => machine.Id == workOrder.MachineId && machine.DeletedAt != null),
                    PlannedStartLocal = row.PlannedStartLocal,
                    PlannedStartUtc = row.PlannedStartUtc,
                    EstimateMinutes = (int)workOrder.Estimate.TotalMinutes,
                    IsDelayed = row.Status == WorkOrderStatus.Staged && row.PlannedStartUtc < nowUtc,
                    CreatedAt = workOrder.CreatedAt,
                    RowVersion = workOrder.RowVersion,
                }),
                cancellationToken);
    }

    /// <summary>
    /// Sorts by the delayed flag, which needs the current time, so the static sort map cannot hold it. The ID is the
    /// last key, like in every grid sort.
    /// </summary>
    private static IOrderedQueryable<WorkOrder> SortByDelayed(
        IQueryable<WorkOrder> query,
        SortDirection direction,
        DateTime nowUtc
    )
    {
        var key = WorkOrderCurrentRow.IsDelayed(nowUtc);
        var ordered = direction == SortDirection.Descending ? query.OrderByDescending(key) : query.OrderBy(key);

        return ordered.ThenBy(workOrder => workOrder.Id);
    }
}
