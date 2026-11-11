using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

/// <summary>One search of a batch: the paging, the sort, and the filter of a single work order search.</summary>
public record SearchWorkOrderBatchEntry : GridRequest<WorkOrderGridFilter, WorkOrderSortColumn>;

public record SearchWorkOrderBatchRequest : IRequest<SearchWorkOrderBatchResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token, and it applies to every search.</summary>
    public required AccessScope Scope { get; init; }

    public required IReadOnlyList<SearchWorkOrderBatchEntry> Searches { get; init; }
}

public record SearchWorkOrderBatchResponse
{
    /// <summary>The page of each search, in the order of the searches.</summary>
    public required IReadOnlyList<GridResponse<WorkOrderRowResponse>> Pages { get; init; }
}

public sealed class SearchWorkOrderBatchEntryValidator
    : GridRequestValidator<SearchWorkOrderBatchEntry, WorkOrderGridFilter, WorkOrderSortColumn>
{
    public SearchWorkOrderBatchEntryValidator()
        : base(WorkOrderSortColumn.All)
    {
        RuleFor(request => request.Filter)
            .SetValidator(new WorkOrderGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchWorkOrderBatchRequestValidator : AbstractValidator<SearchWorkOrderBatchRequest>
{
    public SearchWorkOrderBatchRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Searches)
            .Must(searches => searches is { Count: >= 1 and <= WorkOrderValidationConstants.BatchMaxSearches })
            .WithMessage(WorkOrderErrors.Validation.BatchSizeInvalid);

        RuleForEach(request => request.Searches)
            .NotNull()
            .WithMessage(WorkOrderErrors.Validation.BatchSearchRequired)
            .SetValidator(new SearchWorkOrderBatchEntryValidator())
            .When(request => request.Searches is not null);
    }
}

public sealed class SearchWorkOrderBatchPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchWorkOrderBatchRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchWorkOrderBatchRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page for each search of the batch, in the order of the searches. Every search runs the query of the
/// single search (<see cref="WorkOrderSearchQuery"/>) with the one access scope of the request, so the own scope and
/// the company scope apply to each search. The searches run one after the other, because one database context
/// serves one query at a time.
/// </summary>
/// <param name="query">Runs one search.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope, once for the batch.</param>
public sealed class SearchWorkOrderBatchRequestHandler(WorkOrderSearchQuery query, OwnEmployeeResolver ownEmployee)
    : IRequestHandler<SearchWorkOrderBatchRequest, SearchWorkOrderBatchResponse>
{
    private readonly WorkOrderSearchQuery _query = query;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;

    public async Task<SearchWorkOrderBatchResponse> HandleAsync(
        SearchWorkOrderBatchRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;
        var pages = new List<GridResponse<WorkOrderRowResponse>>(request.Searches.Count);

        foreach (var search in request.Searches)
        {
            pages.Add(
                await _query.SearchAsync(search, request.Scope, request.CompanyId, ownEmployeeId, cancellationToken));
        }

        return new SearchWorkOrderBatchResponse { Pages = pages };
    }
}
