using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
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
/// Dismisses one metric, status, or phase draft of the company that is <c>pending</c> or <c>awaitingTarget</c>. A
/// draft of another company, or an unknown ID, is "not found" (<c>404</c>). A draft that is already applied or
/// dismissed is a business rule against the stored state, so it is "unprocessable" (<c>422</c>, the key
/// <c>draftNotPending</c>). A metric or status draft that an open phase draft of the same proposal still uses is
/// "unprocessable" too (the key <c>draftUsedByPhaseDraft</c>): the user dismisses the phase draft first. A draft that
/// changed in parallel is a conflict (<c>409</c>).
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
            await EnsureNotUsedByOpenPhaseDraftAsync(metric, cancellationToken);
            metric.Dismiss();
        }
        else
        {
            await DismissStatusOrPhaseAsync(request, cancellationToken);
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

    private async Task DismissStatusOrPhaseAsync(
        DismissDefinitionDraftRequest request,
        CancellationToken cancellationToken
    )
    {
        var status = await _dbContext.StatusDefinitionDrafts.SingleOrDefaultAsync(
            candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
            cancellationToken);

        if (status is not null)
        {
            EnsureOpen(status.State);
            await EnsureNotUsedByOpenPhaseDraftAsync(status, cancellationToken);
            status.Dismiss();

            return;
        }

        var phase = await _dbContext.OperatingPhaseDrafts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(DefinitionDraftErrors.DraftNotFound);
        EnsureOpen(phase.State);
        phase.Dismiss();
    }

    private async Task EnsureNotUsedByOpenPhaseDraftAsync(IDefinitionDraft draft, CancellationToken cancellationToken)
    {
        var phases = await _dbContext.OperatingPhaseDrafts
            .AsNoTracking()
            .Include(phase => phase.Conditions)
            .Include(phase => phase.Limits)
            .Where(phase => phase.CompanyId == draft.CompanyId && phase.ProposalEntryId == draft.ProposalEntryId)
            .Where(OpenDefinitionDrafts.Predicate<OperatingPhaseDraft>())
            .ToListAsync(cancellationToken);

        if (phases.Any(phase => phase.UsesDraft(draft.Id)))
        {
            throw new UnprocessableException(DefinitionDraftErrors.DraftUsedByPhaseDraft);
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
