using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record GetRunToolCallsRequest : IRequest<CaseToolCallsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid RunId { get; init; }
}

public sealed class GetRunToolCallsRequestValidator : AbstractValidator<GetRunToolCallsRequest>
{
    public GetRunToolCallsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.CaseId).NotEmpty().WithMessage(CaseErrors.CaseIdRequired);
        RuleFor(request => request.RunId).NotEmpty().WithMessage(CaseErrors.RunIdRequired);
    }
}

public sealed class GetRunToolCallsPreHandlerCheck(ICurrentUserProvider currentUser, IInsightsDbContext dbContext)
    : IPreHandlerCheck<GetRunToolCallsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task CheckAsync(GetRunToolCallsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureCaseInScopeAsync(request.CompanyId, request.CaseId, cancellationToken);
    }
}

/// <summary>
/// Reads the tool calls of one run of a case, in the order of their sequence. A case of another company, and a run
/// that belongs to another case, are "not found".
/// </summary>
/// <param name="dbContext">Reads the case, the run, and its tool calls.</param>
public sealed class GetRunToolCallsRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<GetRunToolCallsRequest, CaseToolCallsResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<CaseToolCallsResponse> HandleAsync(
        GetRunToolCallsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isRunFound = await _dbContext.AgentRuns
            .AsNoTracking()
            .AnyAsync(run => run.Id == request.RunId && run.CaseId == request.CaseId, cancellationToken);

        if (!isRunFound)
        {
            throw new NotFoundException(CaseErrors.RunNotFound);
        }

        var toolCalls = await _dbContext.AgentToolCalls
            .AsNoTracking()
            .Where(call => call.RunId == request.RunId)
            .OrderBy(call => call.Sequence)
            .Select(call => new CaseToolCallResponse
            {
                Sequence = call.Sequence,
                ToolName = call.ToolName,
                Outcome = call.Outcome,
                Arguments = call.Arguments,
                Result = call.Result,
                DurationMilliseconds = call.DurationMilliseconds,
            })
            .ToListAsync(cancellationToken);

        return new CaseToolCallsResponse { ToolCalls = toolCalls };
    }
}
