using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record GetWorkOrderHistoryRequest : IRequest<IReadOnlyList<WorkOrderHistoryResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public record WorkOrderHistoryResponse
{
    public required int Revision { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStartLocal { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required Guid CreatedBy { get; init; }
}

public class GetWorkOrderHistoryRequestValidator : AbstractValidator<GetWorkOrderHistoryRequest>
{
    public GetWorkOrderHistoryRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);
    }
}

public sealed class GetWorkOrderHistoryPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetWorkOrderHistoryRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetWorkOrderHistoryRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads every history row of one work order, the newest first. A work order outside the access scope is "not found".
/// </summary>
/// <param name="dbContext">Reads the work order and its history.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class GetWorkOrderHistoryRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetWorkOrderHistoryRequest, IReadOnlyList<WorkOrderHistoryResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<IReadOnlyList<WorkOrderHistoryResponse>> HandleAsync(
        GetWorkOrderHistoryRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;

        var isReachable = await _dbContext.WorkOrders
            .AsNoTracking()
            .Where(workOrder => workOrder.Id == request.Id)
            .ApplyScope(request.Scope, request.CompanyId, ownEmployeeId, _currentUser.GetUserId())
            .AnyAsync(cancellationToken);
        if (!isReachable)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);
        }

        return await _dbContext.WorkOrderHistories
            .AsNoTracking()
            .Where(row => row.WorkOrderId == request.Id)
            .OrderByDescending(row => row.Revision)
            .Select(row => new WorkOrderHistoryResponse
            {
                Revision = row.Revision,
                Status = row.Status,
                AssigneeId = row.AssigneeId,
                AssigneeName = _dbContext.Employees
                    .Where(employee => employee.Id == row.AssigneeId)
                    .Select(employee => employee.FirstName + " " + employee.LastName)
                    .FirstOrDefault(),
                PlannedStartLocal = row.PlannedStartLocal,
                PlannedStartUtc = row.PlannedStartUtc,
                CreatedAt = row.CreatedAt,
                CreatedBy = row.CreatedBy,
            })
            .ToListAsync(cancellationToken);
    }
}
