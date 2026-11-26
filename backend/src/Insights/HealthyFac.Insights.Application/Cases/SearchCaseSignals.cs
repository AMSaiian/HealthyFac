using FluentValidation;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record SearchCaseSignalsRequest : IRequest<GridResponse<CaseSignalResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid CaseId { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }
}

public sealed class SearchCaseSignalsRequestValidator : AbstractValidator<SearchCaseSignalsRequest>
{
    public SearchCaseSignalsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.CaseId).NotEmpty().WithMessage(CaseErrors.CaseIdRequired);

        RuleFor(request => request.Offset)
            .GreaterThanOrEqualTo(GridValidationConstants.MinOffset)
            .WithMessage(GridErrors.Validation.OffsetOutOfRange);

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);
    }
}

public sealed class SearchCaseSignalsPreHandlerCheck(ICurrentUserProvider currentUser, IInsightsDbContext dbContext)
    : IPreHandlerCheck<SearchCaseSignalsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task CheckAsync(SearchCaseSignalsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureCaseInScopeAsync(request.CompanyId, request.CaseId, cancellationToken);
    }
}

/// <summary>
/// Reads one page of the signals of a case: the open signals first, then the others, each group with the newest start
/// first. A case of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the case and its signals.</param>
public sealed class SearchCaseSignalsRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<SearchCaseSignalsRequest, GridResponse<CaseSignalResponse>>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<GridResponse<CaseSignalResponse>> HandleAsync(
        SearchCaseSignalsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.MachineSignals
            .AsNoTracking()
            .Where(signal => signal.CaseId == request.CaseId && signal.CompanyId == request.CompanyId)
            .OrderBy(signal => signal.State == SignalState.Open ? 0 : 1)
            .ThenByDescending(signal => signal.OpenedAt)
            .ThenBy(signal => signal.Id)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                signal => new CaseSignalResponse
                {
                    Id = signal.Id,
                    Rule = signal.Rule,
                    Severity = signal.Severity,
                    State = signal.State,
                    Coverage = signal.Coverage,
                    MetricKey = signal.MetricKey,
                    StatusCode = signal.StatusCode,
                    Value = signal.Value,
                    Limit = signal.Limit,
                    OpenedAt = signal.OpenedAt,
                    ResolvedAt = signal.ResolvedAt,
                    WorkOrderDraftId = signal.WorkOrderDraftId,
                    WorkOrderId = signal.WorkOrderId,
                },
                cancellationToken);

        return response;
    }
}
