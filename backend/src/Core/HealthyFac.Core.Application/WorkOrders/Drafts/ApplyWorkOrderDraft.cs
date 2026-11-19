using System.Data;
using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

/// <summary>
/// Applies a draft: the values are the work order values that the user confirmed, with the optional assignee and
/// planned start of the company scope.
/// </summary>
public record ApplyWorkOrderDraftRequest : IRequest<ApplyWorkOrderDraftResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }

    /// <summary>The row version that the client read. Without it, the version that the handler loads counts.</summary>
    public uint? RowVersion { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public required int EstimateMinutes { get; init; }

    public Guid? AssigneeId { get; init; }

    public DateTime? PlannedStartLocal { get; init; }
}

public record ApplyWorkOrderDraftResponse
{
    public required Guid WorkOrderId { get; init; }
}

public class ApplyWorkOrderDraftRequestValidator : AbstractValidator<ApplyWorkOrderDraftRequest>
{
    public ApplyWorkOrderDraftRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.DraftIdRequired);

        RuleFor(request => request.Priority).NotNull().WithMessage(WorkOrderErrors.Validation.PriorityRequired);

        RuleFor(request => request.Category).NotNull().WithMessage(WorkOrderErrors.Validation.CategoryRequired);

        RuleFor(request => request.Title).IsWorkOrderTitle();

        RuleFor(request => request.Description).IsWorkOrderDescription(richTextPolicy);

        RuleFor(request => request.EstimateMinutes).IsWorkOrderEstimate();

        RuleFor(request => request)
            .Must(request => request.AssigneeId.HasValue == request.PlannedStartLocal.HasValue)
            .When(request => request.Scope == AccessScope.Company)
            .WithMessage(WorkOrderErrors.Validation.AssigneeAndStartTogether);
    }
}

public sealed class ApplyWorkOrderDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ApplyWorkOrderDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ApplyWorkOrderDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Applies one pending work order draft: it creates the work order with the confirmed values through the create rules
/// (<see cref="WorkOrderCreation"/>, so an estimate that is not on the 5-minute grid fails as in create), marks the
/// draft applied, and adds <see cref="WorkOrderDraftApplied"/>, all in one save. An assignee puts the work in one
/// serializable transaction, as the create does. A draft that is not pending is a <c>422</c>. A stale row version, or a
/// parallel change, is a <c>409</c>.
/// </summary>
/// <remarks>
/// The operation loads the draft inside itself, because a transient failure of the serializable transaction runs it
/// again with an empty change tracker.
/// </remarks>
/// <param name="dbContext">Loads the draft and saves the changes.</param>
/// <param name="creation">Runs the create rules and adds the work order.</param>
/// <param name="outbox">Stores the message with the changes.</param>
/// <param name="timeProvider">Provides the time of the apply.</param>
public sealed class ApplyWorkOrderDraftRequestHandler(
    ICoreDbContext dbContext,
    WorkOrderCreation creation,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<ApplyWorkOrderDraftRequest, ApplyWorkOrderDraftResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly WorkOrderCreation _creation = creation;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<ApplyWorkOrderDraftResponse> HandleAsync(
        ApplyWorkOrderDraftRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        try
        {
            var workOrderId = request.AssigneeId.HasValue
                ? await _dbContext.RunInTransactionAsync(
                    token => ApplyAsync(request, token),
                    IsolationLevel.Serializable,
                    cancellationToken)
                : await ApplyAsync(request, cancellationToken);

            return new ApplyWorkOrderDraftResponse { WorkOrderId = workOrderId };
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(WorkOrderDraftErrors.DraftChanged, exception);
        }
    }

    private async Task<Guid> ApplyAsync(ApplyWorkOrderDraftRequest request, CancellationToken cancellationToken)
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

        var createRequest = new CreateWorkOrderRequest
        {
            CompanyId = request.CompanyId,
            Scope = request.Scope,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            MachineId = draft.MachineId,
            EstimateMinutes = request.EstimateMinutes,
            FileIds = [],
            AssigneeId = request.AssigneeId,
            PlannedStartLocal = request.PlannedStartLocal,
        };
        await _creation.EnsureOwnCreateAsync(createRequest, cancellationToken);
        var workOrderId = await _creation.CreateAsync(createRequest, cancellationToken);

        draft.MarkApplied(workOrderId);
        _outbox.Add(
            new WorkOrderDraftApplied
            {
                DraftId = draft.Id,
                WorkOrderId = workOrderId,
                CaseId = draft.CaseId,
                CompanyId = draft.CompanyId,
                AppliedAt = _timeProvider.GetUtcNow().UtcDateTime,
            });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return workOrderId;
    }
}
