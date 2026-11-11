using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record GetWorkOrderRequest : IRequest<WorkOrderResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public class GetWorkOrderRequestValidator : AbstractValidator<GetWorkOrderRequest>
{
    public GetWorkOrderRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);
    }
}

public sealed class GetWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one work order. A work order outside the access scope is "not found", so the answer does not show that it
/// exists. The machine is read with its deleted state: a deleted machine keeps its name and gets a flag.
/// </summary>
/// <param name="dbContext">Reads the work order.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="currentUser">Reads the actor.</param>
/// <param name="timeProvider">Provides the time that decides whether the work order is delayed.</param>
public sealed class GetWorkOrderRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider
) : IRequestHandler<GetWorkOrderRequest, WorkOrderResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<WorkOrderResponse> HandleAsync(
        GetWorkOrderRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;

        string[] deletedFilters = [SoftDeleteConstants.QueryFilterName];
        var row = await _dbContext.WorkOrders
                .AsNoTracking()
                .Where(workOrder => workOrder.Id == request.Id)
                .ApplyScope(request.Scope, request.CompanyId, ownEmployeeId, _currentUser.GetUserId())
                .Select(workOrder => new
                {
                    WorkOrder = workOrder,
                    Current = workOrder.History.First(history => history.Revision == workOrder.CurrentRevision),
                    workOrder.RowVersion,
                    FileIds = workOrder.Files.OrderBy(link => link.CreatedAt).Select(link => link.FileId).ToList(),
                    AssigneeName = _dbContext.Employees
                        .Where(employee => employee.Id
                            == workOrder.History
                                .First(history => history.Revision == workOrder.CurrentRevision)
                                .AssigneeId)
                        .Select(employee => employee.FirstName + " " + employee.LastName)
                        .FirstOrDefault(),
                    MachineName = _dbContext.Machines
                        .IgnoreQueryFilters(deletedFilters)
                        .Where(machine => machine.Id == workOrder.MachineId)
                        .Select(machine => machine.Name)
                        .FirstOrDefault(),
                    IsMachineDeleted = _dbContext.Machines
                        .IgnoreQueryFilters(deletedFilters)
                        .Any(machine => machine.Id == workOrder.MachineId && machine.DeletedAt != null),
                })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return new WorkOrderResponse
        {
            Id = row.WorkOrder.Id,
            Title = row.WorkOrder.Title,
            Description = row.WorkOrder.Description,
            Priority = row.WorkOrder.Priority,
            Category = row.WorkOrder.Category,
            EstimateMinutes = (int)row.WorkOrder.Estimate.TotalMinutes,
            Status = row.Current.Status,
            AssigneeId = row.Current.AssigneeId,
            AssigneeName = row.AssigneeName,
            PlannedStartLocal = row.Current.PlannedStartLocal,
            PlannedStartUtc = row.Current.PlannedStartUtc,
            IsDelayed = row.Current.Status == WorkOrderStatus.Staged && row.Current.PlannedStartUtc < nowUtc,
            MachineId = row.WorkOrder.MachineId,
            MachineName = row.MachineName,
            IsMachineDeleted = row.IsMachineDeleted,
            FileIds = row.FileIds,
            RowVersion = row.RowVersion,
            CreatedAt = row.WorkOrder.CreatedAt,
        };
    }
}
