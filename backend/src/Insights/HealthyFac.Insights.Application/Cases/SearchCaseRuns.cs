using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record SearchCaseRunsRequest : IRequest<GridResponse<CaseRunResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid CaseId { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }
}

public sealed class SearchCaseRunsRequestValidator : AbstractValidator<SearchCaseRunsRequest>
{
    public SearchCaseRunsRequestValidator()
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

public sealed class SearchCaseRunsPreHandlerCheck(ICurrentUserProvider currentUser, IInsightsDbContext dbContext)
    : IPreHandlerCheck<SearchCaseRunsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task CheckAsync(SearchCaseRunsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureCaseInScopeAsync(request.CompanyId, request.CaseId, cancellationToken);
    }
}

/// <summary>
/// Reads one page of the runs of a case, the newest first (the run IDs are time ordered). The rows hold no tool
/// calls. A case of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the case and its runs.</param>
public sealed class SearchCaseRunsRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<SearchCaseRunsRequest, GridResponse<CaseRunResponse>>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<GridResponse<CaseRunResponse>> HandleAsync(
        SearchCaseRunsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.AgentRuns
            .AsNoTracking()
            .Where(run => run.CaseId == request.CaseId)
            .OrderByDescending(run => run.Id)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                run => new CaseRunResponse
                {
                    Id = run.Id,
                    Role = run.Role,
                    State = run.State,
                    Model = run.Model,
                    PromptVersion = run.PromptVersion,
                    AttemptNumber = run.AttemptNumber,
                    InputTokens = run.InputTokens,
                    OutputTokens = run.OutputTokens,
                    Verdict = run.Verdict,
                    VerdictReason = run.VerdictReason,
                    ErrorCode = run.ErrorCode,
                    SkipReason = run.SkipReason,
                    StartedAt = run.StartedAt,
                    FinishedAt = run.FinishedAt,
                },
                cancellationToken);

        return response;
    }
}
