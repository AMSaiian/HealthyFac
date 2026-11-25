using System.Collections.Frozen;
using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

/// <summary>The columns that a grid of work order drafts sorts by.</summary>
public sealed class WorkOrderDraftSortColumn : Enumeration<WorkOrderDraftSortColumn>
{
    public static readonly WorkOrderDraftSortColumn ProposedAt = new(value: "proposedAt");

    public static readonly WorkOrderDraftSortColumn State = new(value: "state");

    private WorkOrderDraftSortColumn(string value)
        : base(value)
    {
    }
}

/// <summary>The filter of the draft grid: the states and the machines.</summary>
public sealed record WorkOrderDraftGridFilter : IFilter<WorkOrderDraft>
{
    public SetFilter<DraftState>? States { get; init; }

    public SetFilter<Guid>? MachineIds { get; init; }

    public Expression<Func<WorkOrderDraft, bool>>? ToPredicate() =>
        Predicates.From(States?.ToPredicate<WorkOrderDraft>(draft => draft.State))
            .And(MachineIds?.ToPredicate<WorkOrderDraft>(draft => draft.MachineId!.Value));
}

public sealed class WorkOrderDraftGridFilterValidator : AbstractValidator<WorkOrderDraftGridFilter>
{
    public WorkOrderDraftGridFilterValidator()
    {
        When(filter => filter.States is not null, () => RuleFor(filter => filter.States!).IsSetFilter());

        When(filter => filter.MachineIds is not null, () => RuleFor(filter => filter.MachineIds!).IsSetFilter());
    }
}

/// <summary>The sort map of the draft grid. The pending drafts come first by default.</summary>
public static class WorkOrderDraftSortMap
{
    public static readonly FrozenDictionary<WorkOrderDraftSortColumn, Expression<Func<WorkOrderDraft, dynamic>>>
        Instance =
            new Dictionary<WorkOrderDraftSortColumn, Expression<Func<WorkOrderDraft, dynamic>>>
            {
                [WorkOrderDraftSortColumn.ProposedAt] = draft => draft.CreatedAt,
                [WorkOrderDraftSortColumn.State] = draft => draft.State == DraftState.Pending
                    ? 0
                    : draft.State == DraftState.Applied
                        ? 1
                        : 2,
            }.ToFrozenDictionary();

    public static readonly GridSort<WorkOrderDraftSortColumn> Default = new()
    {
        Column = WorkOrderDraftSortColumn.State,
        Direction = SortDirection.Ascending,
    };
}

public record SearchWorkOrderDraftsRequest : GridRequest<WorkOrderDraftGridFilter, WorkOrderDraftSortColumn>,
    IRequest<GridResponse<WorkOrderDraftResponse>>
{
    public required Guid CompanyId { get; init; }
}

public sealed class SearchWorkOrderDraftsRequestValidator
    : GridRequestValidator<SearchWorkOrderDraftsRequest, WorkOrderDraftGridFilter, WorkOrderDraftSortColumn>
{
    public SearchWorkOrderDraftsRequestValidator()
        : base(WorkOrderDraftSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Filter!)
            .SetValidator(new WorkOrderDraftGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchWorkOrderDraftsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchWorkOrderDraftsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchWorkOrderDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the work order drafts of the company. The pending drafts come first unless the request names a
/// sort.
/// </summary>
/// <param name="dbContext">Reads the drafts.</param>
public sealed class SearchWorkOrderDraftsRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<SearchWorkOrderDraftsRequest, GridResponse<WorkOrderDraftResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<GridResponse<WorkOrderDraftResponse>> HandleAsync(
        SearchWorkOrderDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.WorkOrderDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort ?? WorkOrderDraftSortMap.Default, WorkOrderDraftSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                WorkOrderDraftResponse.ProjectionFor(_dbContext),
                cancellationToken);

        return response;
    }
}
