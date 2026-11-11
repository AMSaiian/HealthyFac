using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record DismissDraftRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public class DismissDraftRequestValidator : AbstractValidator<DismissDraftRequest>
{
    public DismissDraftRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(DraftErrors.DraftIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DismissDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DismissDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DismissDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Dismisses one pending draft of the company: a machine draft, a metric draft, or a status draft. A create machine
/// draft also dismisses its pending metric and status drafts, because they have no machine without it. An update
/// machine draft dismisses only itself. A draft of another company is "not found", and a draft that is not pending
/// is a conflict.
/// </summary>
/// <param name="dbContext">Loads the drafts and saves the change.</param>
public sealed class DismissDraftRequestHandler(ICoreDbContext dbContext) : RequestHandler<DismissDraftRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(DismissDraftRequest request, CancellationToken cancellationToken)
    {
        var machineDraft = await _dbContext.MachineDrafts.SingleOrDefaultAsync(
            candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
            cancellationToken);

        if (machineDraft is not null)
        {
            await DismissMachineDraftAsync(machineDraft, cancellationToken);
        }
        else
        {
            await DismissChildDraftAsync(request, cancellationToken);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DraftErrors.DraftChanged, exception);
        }
    }

    private async Task DismissMachineDraftAsync(MachineDraft draft, CancellationToken cancellationToken)
    {
        EnsurePending(draft.State);
        draft.Dismiss();

        if (!draft.IsCreate)
        {
            return;
        }

        var metrics = await _dbContext.MetricDefinitionDrafts
            .Where(child => child.TargetMachineDraftId == draft.Id && child.State == DraftState.Pending)
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.StatusDefinitionDrafts
            .Where(child => child.TargetMachineDraftId == draft.Id && child.State == DraftState.Pending)
            .ToListAsync(cancellationToken);
        metrics.ForEach(child => child.Dismiss());
        statuses.ForEach(child => child.Dismiss());
    }

    private async Task DismissChildDraftAsync(DismissDraftRequest request, CancellationToken cancellationToken)
    {
        var metric = await _dbContext.MetricDefinitionDrafts.SingleOrDefaultAsync(
            candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
            cancellationToken);

        if (metric is not null)
        {
            EnsurePending(metric.State);
            metric.Dismiss();

            return;
        }

        var status = await _dbContext.StatusDefinitionDrafts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(DraftErrors.DraftNotFound);
        EnsurePending(status.State);
        status.Dismiss();
    }

    private static void EnsurePending(DraftState state)
    {
        if (state != DraftState.Pending)
        {
            throw new ConflictException(DraftErrors.DraftNotPending);
        }
    }
}
