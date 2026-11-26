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
public sealed class RecordWorkOrderDraftDismissedPreHandlerCheck : PassThroughPreHandlerCheck<WorkOrderDraftDismissed>;

/// <summary>
/// Sets the signals that a dismissed work order draft covered from <c>drafted</c> to <c>dismissed</c>. It queues no
/// run: a person decided that no work order is needed. A repeat of the message finds no <c>drafted</c> signal. The
/// handler opens no transaction: the inbox runner calls it inside its own transaction.
/// </summary>
/// <param name="dbContext">Loads the signals and saves the change.</param>
public sealed class RecordWorkOrderDraftDismissedRequestHandler(IInsightsDbContext dbContext)
    : RequestHandler<WorkOrderDraftDismissed>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        WorkOrderDraftDismissed request,
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

        proposal?.FollowDismissed();
        signals.ForEach(signal => signal.Dismiss());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
