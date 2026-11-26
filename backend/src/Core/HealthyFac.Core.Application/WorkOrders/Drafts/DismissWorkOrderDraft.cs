using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

public record DismissWorkOrderDraftRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The row version that the client read. Without it, the version that the handler loads counts.</summary>
    public uint? RowVersion { get; init; }
}

public class DismissWorkOrderDraftRequestValidator : AbstractValidator<DismissWorkOrderDraftRequest>
{
    public DismissWorkOrderDraftRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.DraftIdRequired);
    }
}

public sealed class DismissWorkOrderDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DismissWorkOrderDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DismissWorkOrderDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Dismisses one pending work order draft of the company, and tells Insights with <see cref="WorkOrderDraftDismissed"/>
/// in the same save. A draft that is not pending is a <c>422</c>. A stale row version, or a parallel change, is a
/// <c>409</c>.
/// </summary>
/// <param name="dbContext">Loads the draft and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time of the dismiss.</param>
public sealed class DismissWorkOrderDraftRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<DismissWorkOrderDraftRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        DismissWorkOrderDraftRequest request,
        CancellationToken cancellationToken
    )
    {
        var draft = await _dbContext.WorkOrderDrafts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(WorkOrderDraftErrors.DraftNotFound);

        if (request.RowVersion is { } rowVersion)
        {
            _dbContext.Entry(draft).Property(stored => stored.RowVersion).OriginalValue = rowVersion;
        }

        if (draft.State != DraftState.Pending)
        {
            throw new UnprocessableException(WorkOrderDraftErrors.DraftNotPending);
        }

        draft.Dismiss();

        _outbox.Add(
            new WorkOrderDraftDismissed
            {
                DraftId = draft.Id,
                CaseId = draft.CaseId,
                CompanyId = draft.CompanyId,
                DismissedAt = _timeProvider.GetUtcNow().UtcDateTime,
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(WorkOrderDraftErrors.DraftChanged, exception);
        }
    }
}
