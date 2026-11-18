using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record DismissDefinitionDraftRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public sealed class DismissDefinitionDraftRequestValidator : AbstractValidator<DismissDefinitionDraftRequest>
{
    public DismissDefinitionDraftRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(DefinitionDraftErrors.DraftIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DismissDefinitionDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DismissDefinitionDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DismissDefinitionDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Dismisses one metric or status draft of the company that is <c>pending</c> or <c>awaitingTarget</c>. A draft of
/// another company, or an unknown ID, is "not found" (<c>404</c>). A draft that is already applied or dismissed is a
/// business rule against the stored state, so it is "unprocessable" (<c>422</c>, the key <c>draftNotPending</c>). A
/// draft that changed in parallel is a conflict (<c>409</c>).
/// </summary>
/// <param name="dbContext">Loads the draft and saves the change.</param>
public sealed class DismissDefinitionDraftRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<DismissDefinitionDraftRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        DismissDefinitionDraftRequest request,
        CancellationToken cancellationToken
    )
    {
        var metric = await _dbContext.MetricDefinitionDrafts.SingleOrDefaultAsync(
            candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
            cancellationToken);

        if (metric is not null)
        {
            EnsureOpen(metric.State);
            metric.Dismiss();
        }
        else
        {
            var status = await _dbContext.StatusDefinitionDrafts.SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
                ?? throw new NotFoundException(DefinitionDraftErrors.DraftNotFound);
            EnsureOpen(status.State);
            status.Dismiss();
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DefinitionDraftErrors.DraftChanged, exception);
        }
    }

    private static void EnsureOpen(DefinitionDraftState state)
    {
        if (state != DefinitionDraftState.Pending && state != DefinitionDraftState.AwaitingTarget)
        {
            throw new UnprocessableException(DefinitionDraftErrors.DraftNotPending);
        }
    }
}
