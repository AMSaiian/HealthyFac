using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// The create rules of a work order, shared by the create use case and the apply of a work order draft: the check of
/// the own scope, the machine check, the new work order with its files, and the schedule checks of an assignee. It adds
/// the work order to the context and never saves, so the caller saves it with its own changes in one save.
/// </summary>
/// <param name="dbContext">Reads the machine and tracks the new work order.</param>
/// <param name="scheduleChecks">Checks the slot of an assigned work order.</param>
/// <param name="ownEmployee">Finds the employee of the actor: the own scope needs one.</param>
public sealed class WorkOrderCreation(
    ICoreDbContext dbContext,
    ScheduleChecks scheduleChecks,
    OwnEmployeeResolver ownEmployee
)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;

    /// <summary>
    /// Checks the rules of the own scope: it may set no assignee and no start, and the actor needs an employee.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>A task that completes when the check passes.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when the own scope sets an assignee or a start.</exception>
    /// <exception cref="NotFoundException">Thrown when the actor has no employee.</exception>
    public async Task EnsureOwnCreateAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Scope != AccessScope.Own)
        {
            return;
        }

        if (request.AssigneeId.HasValue || request.PlannedStartLocal.HasValue)
        {
            throw new ForbiddenAccessException(WorkOrderErrors.Rejections.OwnScopeForbidden);
        }

        if (await _ownEmployee.FindIdAsync(cancellationToken) is null)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.OwnEmployeeNotFound);
        }
    }

    /// <summary>
    /// Creates the work order and adds it to the context. It does not save.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The token that cancels the queries.</param>
    /// <returns>The ID of the new work order.</returns>
    /// <exception cref="NotFoundException">Thrown when the machine is not a machine of the company.</exception>
    public async Task<Guid> CreateAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.MachineId is { } machineId)
        {
            var isMachineInCompany = await _dbContext.Machines
                .AsNoTracking()
                .AnyAsync(
                    machine => machine.Id == machineId && machine.CompanyId == request.CompanyId,
                    cancellationToken);
            if (!isMachineInCompany)
            {
                throw new NotFoundException(WorkOrderErrors.Rejections.MachineNotFound);
            }
        }

        var estimate = TimeSpan.FromMinutes(request.EstimateMinutes);
        var workOrder = WorkOrder.Create(
            request.CompanyId,
            request.Title,
            request.Description,
            request.Priority,
            request.Category,
            request.MachineId,
            estimate);
        workOrder.ReplaceFiles(request.FileIds);

        if (request is { AssigneeId: { } assigneeId, PlannedStartLocal: { } plannedStartLocal })
        {
            var startUtc = await _scheduleChecks.EnsureAsync(
                request.CompanyId,
                assigneeId,
                plannedStartLocal,
                estimate,
                workOrder.Id,
                cancellationToken);
            workOrder.Assign(assigneeId, plannedStartLocal, startUtc);
        }

        _dbContext.WorkOrders.Add(workOrder);

        return workOrder.Id;
    }
}
