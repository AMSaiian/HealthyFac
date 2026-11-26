using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>
/// The message carries the company, and the handler filters the signals by the case and the draft of the message, so
/// no scope check is needed. The actor of the inbox has no company, so the check cannot compare the company of the
/// actor.
/// </summary>
public sealed class RecordWorkOrderDraftAppliedPreHandlerCheck : PassThroughPreHandlerCheck<WorkOrderDraftApplied>;

/// <summary>
/// Moves the signals that a work order draft covers from <c>drafted</c> to <c>linked</c> with the work order that a
/// user created from the draft. A repeat of the message finds no <c>drafted</c> signal and changes nothing. The
/// handler opens no transaction: the inbox runner calls it inside its own transaction.
/// </summary>
/// <param name="dbContext">Loads the signals and saves the change.</param>
public sealed class RecordWorkOrderDraftAppliedRequestHandler(IInsightsDbContext dbContext)
    : RequestHandler<WorkOrderDraftApplied>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        WorkOrderDraftApplied request,
        CancellationToken cancellationToken
    )
    {
        var proposal = await _dbContext.AgentActions
            .SingleOrDefaultAsync(
                action => action.Id == request.DraftId && action.Kind == AgentActionKind.ProposeWorkOrder,
                cancellationToken);

        var signals = await _dbContext.MachineSignals
            .Where(signal => signal.WorkOrderDraftId == request.DraftId
                && signal.CaseId == request.CaseId
                && signal.Coverage == SignalCoverage.Drafted)
            .ToListAsync(cancellationToken);

        proposal?.FollowApplied(request.WorkOrderId);
        signals.ForEach(signal => signal.MarkLinked(request.WorkOrderId));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
