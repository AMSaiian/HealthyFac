using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record GetWorkOrderHistoryRequest : IRequest<WorkOrderHistoryPageResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The page size.</summary>
    public required int Limit { get; init; }

    /// <summary>
    /// The revision of the last row of the previous page. The page holds the rows with an older revision, or the
    /// newest rows when it is <see langword="null"/>.
    /// </summary>
    public int? After { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public record WorkOrderHistoryPageResponse
{
    public required IReadOnlyList<WorkOrderHistoryResponse> Items { get; init; }

    /// <summary>The number of history rows of the work order, before the page is cut.</summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// The revision to send as <c>after</c> for the next page, or <see langword="null"/> when no older row exists.
    /// </summary>
    public int? NextAfter { get; init; }
}

public record WorkOrderHistoryResponse
{
    public required int Revision { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStartLocal { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    /// <summary>The planned working time from this row on, in whole minutes.</summary>
    public required int EstimateMinutes { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required Guid CreatedBy { get; init; }
}

public class GetWorkOrderHistoryRequestValidator : AbstractValidator<GetWorkOrderHistoryRequest>
{
    public GetWorkOrderHistoryRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);
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
/// Reads one page of the history of a work order, the newest row first, after the revision of the previous page. A
/// work order outside the access scope is "not found". The revision is unique per work order, so it alone is the
/// position.
/// </summary>
/// <param name="dbContext">Reads the work order and its history.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class GetWorkOrderHistoryRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetWorkOrderHistoryRequest, WorkOrderHistoryPageResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<WorkOrderHistoryPageResponse> HandleAsync(
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

        var rows = _dbContext.WorkOrderHistories
            .AsNoTracking()
            .Where(row => row.WorkOrderId == request.Id);
        var totalCount = await rows.CountAsync(cancellationToken);

        var page = await rows
            .Where(row => request.After == null || row.Revision < request.After)
            .OrderByDescending(row => row.Revision)
            .Take(request.Limit + 1)
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
                EstimateMinutes = (int)row.Estimate.TotalMinutes,
                CreatedAt = row.CreatedAt,
                CreatedBy = row.CreatedBy,
            })
            .ToListAsync(cancellationToken);

        var hasOlderRow = page.Count > request.Limit;
        var items = page.Take(request.Limit).ToList();

        return new WorkOrderHistoryPageResponse
        {
            Items = items,
            TotalCount = totalCount,
            NextAfter = hasOlderRow ? items[^1].Revision : null,
        };
    }
}
