using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

public record SearchWorkOrdersRequest : GridRequest<WorkOrderGridFilter, WorkOrderSortColumn>,
    IRequest<GridResponse<WorkOrderRowResponse>>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public record WorkOrderRowResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public Guid? MachineId { get; init; }

    public string? MachineName { get; init; }

    public required bool IsMachineDeleted { get; init; }

    public DateTime? PlannedStartLocal { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    public required int EstimateMinutes { get; init; }

    public required bool IsDelayed { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required uint RowVersion { get; init; }
}

public sealed class SearchWorkOrdersRequestValidator
    : GridRequestValidator<SearchWorkOrdersRequest, WorkOrderGridFilter, WorkOrderSortColumn>
{
    public SearchWorkOrdersRequestValidator()
        : base(WorkOrderSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new WorkOrderGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchWorkOrdersPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchWorkOrdersRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchWorkOrdersRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the work orders that the access scope reaches (<see cref="WorkOrderSearchQuery"/>).
/// </summary>
/// <param name="query">Runs the search.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
public sealed class SearchWorkOrdersRequestHandler(WorkOrderSearchQuery query, OwnEmployeeResolver ownEmployee)
    : IRequestHandler<SearchWorkOrdersRequest, GridResponse<WorkOrderRowResponse>>
{
    private readonly WorkOrderSearchQuery _query = query;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;

    public async Task<GridResponse<WorkOrderRowResponse>> HandleAsync(
        SearchWorkOrdersRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;

        return await _query.SearchAsync(request, request.Scope, request.CompanyId, ownEmployeeId, cancellationToken);
    }
}
