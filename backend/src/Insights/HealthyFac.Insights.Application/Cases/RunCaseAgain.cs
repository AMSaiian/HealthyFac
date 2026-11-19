using FluentValidation;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record RunCaseAgainRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public sealed class RunCaseAgainRequestValidator : AbstractValidator<RunCaseAgainRequest>
{
    public RunCaseAgainRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(CaseErrors.CaseIdRequired);
    }
}

public sealed class RunCaseAgainPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<RunCaseAgainRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(RunCaseAgainRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Queues a triage run, due now, for an open case. A case with a queued run gets no second one. A case that is not
/// open is an error, because a closed or resolved case needs no run.
/// </summary>
/// <param name="dbContext">Reads the case and saves the run.</param>
/// <param name="timeProvider">Provides the due time of the run.</param>
public sealed class RunCaseAgainRequestHandler(IInsightsDbContext dbContext, TimeProvider timeProvider)
    : RequestHandler<RunCaseAgainRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        RunCaseAgainRequest request,
        CancellationToken cancellationToken
    )
    {
        var maintenanceCase = await _dbContext.MaintenanceCases
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(CaseErrors.CaseNotFound);

        if (maintenanceCase.State != CaseState.Open)
        {
            throw new UnprocessableException(CaseErrors.CaseNotOpen);
        }

        var hasQueuedRun = await _dbContext.AgentRuns.AnyAsync(
            run => run.CaseId == request.Id && run.State == AgentRunState.Queued,
            cancellationToken);

        if (hasQueuedRun)
        {
            return;
        }

        _dbContext.AgentRuns.Add(
            new AgentRun(
                AgentRole.Triage,
                request.Id,
                ingestAttemptId: null,
                _timeProvider.GetUtcNow().UtcDateTime));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
