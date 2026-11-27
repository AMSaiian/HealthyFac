using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

/// <summary>
/// Checks that the proposal belongs to the company of the restored actor (a mismatch is a <c>403</c>), and that the
/// machine of the message is an active machine of that company (a miss is a <c>404</c>), so the output of the agent
/// never reaches another company, even when the message holds a wrong ID.
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class WorkOrderDraftProposedPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<WorkOrderDraftProposed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(WorkOrderDraftProposed request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is not { } machineId)
        {
            return;
        }

        var isMachineOfCompany = await _dbContext.Machines.AnyAsync(
            machine => machine.Id == machineId && machine.CompanyId == request.CompanyId,
            cancellationToken);

        if (!isMachineOfCompany)
        {
            throw new NotFoundException(WorkOrderDraftErrors.MachineNotFound);
        }
    }
}

/// <summary>
/// Stores the work order that the planner proposed, as a pending draft. The draft ID is the ID of the agent action, so
/// a repeat delivery finds the draft and stores nothing new ("find, else create").
/// </summary>
/// <param name="dbContext">Finds the draft and saves the new one.</param>
/// <param name="notifier">Adds the real-time event of a new draft. The inbox runner pushes it after the commit.</param>
public sealed class StoreProposedWorkOrderDraftRequestHandler(ICoreDbContext dbContext, IRealtimeNotifier notifier)
    : RequestHandler<WorkOrderDraftProposed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        WorkOrderDraftProposed request,
        CancellationToken cancellationToken
    )
    {
        var isKnown = await _dbContext.WorkOrderDrafts.AnyAsync(
            draft => draft.Id == request.DraftId && draft.CompanyId == request.CompanyId,
            cancellationToken);

        if (isKnown)
        {
            return;
        }

        var isCase = request.Source == WorkOrderProposalSource.Case;
        var proposalId = (isCase ? request.RunId : request.ChatMessageId)!.Value;

        var isProposalStored = await _dbContext.DraftProposals
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AnyAsync(proposal => proposal.Id == proposalId, cancellationToken);

        if (!isProposalStored)
        {
            _dbContext.DraftProposals.Add(
                isCase
                    ? DraftProposal.ForCase(proposalId, request.CompanyId, request.CaseId!.Value)
                    : DraftProposal.ForChat(
                        proposalId, request.CompanyId, request.ChatId!.Value, request.RequestedByName));
        }

        _dbContext.WorkOrderDrafts.Add(
            new WorkOrderDraft(
                request.DraftId,
                request.CompanyId,
                request.MachineId,
                proposalId,
                request.Title,
                request.Description,
                WorkOrderPriority.FromValue(request.Priority),
                WorkOrderCategory.FromValue(request.Category),
                request.EstimateMinutes));

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!isCase)
        {
            return;
        }

        _notifier.AddToGroup(
            request.CompanyId,
            RealtimeNotificationKind.WorkOrderDraftCreated,
            new WorkOrderDraftCreatedData { DraftId = request.DraftId, Title = request.Title },
            isActorSkipped: false);
    }
}
