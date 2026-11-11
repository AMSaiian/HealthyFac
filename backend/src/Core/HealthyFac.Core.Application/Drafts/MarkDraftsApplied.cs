using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record MarkDraftsAppliedRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required IReadOnlyList<Guid> Ids { get; init; }
}

public class MarkDraftsAppliedRequestValidator : AbstractValidator<MarkDraftsAppliedRequest>
{
    public MarkDraftsAppliedRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);

        RuleFor(x => x.Ids)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(DraftErrors.IdsRequired)
            .Must(ids => ids.Count is >= 1 and <= DraftValidationConstants.MarkAppliedMaxIds)
            .WithMessage(DraftErrors.IdsCount)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage(DraftErrors.DraftIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class MarkDraftsAppliedPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<MarkDraftsAppliedRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(MarkDraftsAppliedRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marks metric and status drafts of the company as applied, after the user applied them through Telemetry. The call
/// is all or nothing: the handler checks every draft first and changes none when one fails. A draft that is already
/// applied stays applied, so a repeat call is safe. A pending draft that waits on a create machine draft is a
/// conflict (<see cref="DraftErrors.MachineDraftNotApplied"/>), and a dismissed draft is a conflict
/// (<see cref="DraftErrors.DraftNotPending"/>). A draft of another company, or an ID that is no metric or status
/// draft, is "not found".
/// </summary>
/// <param name="dbContext">Loads the drafts and saves the change.</param>
public sealed class MarkDraftsAppliedRequestHandler(ICoreDbContext dbContext) : RequestHandler<MarkDraftsAppliedRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        MarkDraftsAppliedRequest request,
        CancellationToken cancellationToken
    )
    {
        var ids = request.Ids.Distinct().ToList();
        var metrics = await _dbContext.MetricDefinitionDrafts
            .Where(draft => ids.Contains(draft.Id) && draft.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.StatusDefinitionDrafts
            .Where(draft => ids.Contains(draft.Id) && draft.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);

        if (metrics.Count + statuses.Count != ids.Count)
        {
            throw new NotFoundException(DraftErrors.DraftNotFound);
        }

        metrics.ForEach(draft => EnsureMarkable(draft.State, draft.TargetMachineDraftId));
        statuses.ForEach(draft => EnsureMarkable(draft.State, draft.TargetMachineDraftId));

        foreach (var metric in metrics.Where(draft => draft.State == DraftState.Pending))
        {
            metric.MarkApplied();
        }

        foreach (var status in statuses.Where(draft => draft.State == DraftState.Pending))
        {
            status.MarkApplied();
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

    private static void EnsureMarkable(DraftState state, Guid? targetMachineDraftId)
    {
        if (state == DraftState.Dismissed)
        {
            throw new ConflictException(DraftErrors.DraftNotPending);
        }

        if (state == DraftState.Pending && targetMachineDraftId is not null)
        {
            throw new ConflictException(DraftErrors.MachineDraftNotApplied);
        }
    }
}
