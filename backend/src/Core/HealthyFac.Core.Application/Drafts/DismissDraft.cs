using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
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
/// Dismisses one pending machine draft of the company. A draft of another company, or an ID that is not a machine
/// draft (a metric or status draft lives in Telemetry), is "not found". A draft that is not pending is unprocessable
/// (422). A create draft also queues <see cref="MachineDraftDismissed"/> in the same save, so Telemetry dismisses the
/// metric and status drafts that wait on it.
/// </summary>
/// <param name="dbContext">Loads the draft and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
public sealed class DismissDraftRequestHandler(ICoreDbContext dbContext, IOutbox outbox)
    : RequestHandler<DismissDraftRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(DismissDraftRequest request, CancellationToken cancellationToken)
    {
        var machineDraft = await _dbContext.MachineDrafts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(DraftErrors.DraftNotFound);

        if (machineDraft.State != DraftState.Pending)
        {
            throw new UnprocessableException(DraftErrors.DraftNotPending);
        }

        machineDraft.Dismiss();

        if (machineDraft.IsCreate)
        {
            _outbox.Add(new MachineDraftDismissed { DraftId = machineDraft.Id, CompanyId = machineDraft.CompanyId });
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
}
