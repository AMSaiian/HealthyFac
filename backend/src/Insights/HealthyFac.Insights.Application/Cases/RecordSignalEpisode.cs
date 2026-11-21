using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Realtime;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Messaging.Signals;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>
/// The message carries the company, and the handler filters every query by the machine of the message, so no scope
/// check is needed. The actor of the inbox has no company, so the check cannot compare the company of the actor.
/// </summary>
public sealed class RecordSignalEpisodePreHandlerCheck : PassThroughPreHandlerCheck<SignalEpisodeChanged>;

/// <summary>
/// Stores a signal episode of Telemetry as a machine signal and puts it into the open case of its machine.
/// </summary>
/// <remarks>
/// A message whose sequence is not above the stored one changes nothing, so a repeat and a message in another order do
/// no harm. An open signal without a case joins the open case of the machine, or opens a case that takes over the open
/// signals of the machine that are not <c>dismissed</c>, also the open signals of a closed case (a
/// <c>drafted</c> or <c>linked</c> signal keeps its coverage). An escalation of a <c>dismissed</c>
/// signal makes it <c>new</c> again and moves it to the open case, or to a new case. A resolve lets the case resolve
/// when no signal of it is open. An open signal and an escalation of an uncovered or a <c>dismissed</c> signal queue a
/// triage run. A resolve and an escalation of a <c>drafted</c> or a <c>linked</c> signal queue none. The handler
/// opens no transaction: the inbox runner calls it inside its own transaction (<c>ReadCommitted</c>) with the
/// claimed inbox row, and a second transaction on the same connection fails. Two races end in a failed save, so the
/// inbox retries the message. Two messages that open a case for one machine meet the unique index of the open case.
/// A join and a resolve of one case, and two resolves of one case, meet the row version of the case, because each of
/// them marks the case row as changed (<see cref="MarkCaseTouched"/>).
/// </remarks>
/// <param name="dbContext">Loads and changes the signals and the cases.</param>
/// <param name="runQueue">Queues the triage run.</param>
/// <param name="timeProvider">Provides the time of the case and the run.</param>
/// <param name="notifier">Adds the case events after the save. The inbox runner pushes them after the commit.</param>
public sealed class RecordSignalEpisodeRequestHandler(
    IInsightsDbContext dbContext,
    AgentRunQueue runQueue,
    TimeProvider timeProvider,
    IRealtimeNotifier notifier
) : RequestHandler<SignalEpisodeChanged>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly AgentRunQueue _runQueue = runQueue;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override Task HandleInternalAsync(SignalEpisodeChanged request, CancellationToken cancellationToken) =>
        RecordAsync(request, cancellationToken);

    private static SignalChange ToChange(SignalEpisodeChanged message) =>
        new()
        {
            Rule = SignalRule.FromValue(message.Rule.Value),
            Severity = SignalSeverity.FromValue(message.Severity.Value),
            State = SignalState.FromValue(message.State.Value),
            MetricDefinitionId = message.MetricDefinitionId,
            MetricKey = message.MetricKey,
            StatusDefinitionId = message.StatusDefinitionId,
            StatusCode = message.StatusCode,
            PhaseCode = message.PhaseCode,
            Value = message.Value,
            Limit = message.Limit,
            Z = message.Z,
            ForecastSeconds = message.ForecastSeconds,
            OpenedAt = message.OpenedAt,
            ResolvedAt = message.ResolvedAt,
            Sequence = message.Sequence,
        };

    private async Task RecordAsync(SignalEpisodeChanged message, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var change = ToChange(message);
        var signal = await _dbContext.MachineSignals.SingleOrDefaultAsync(
            candidate => candidate.Id == message.EpisodeId && candidate.CompanyId == message.CompanyId,
            cancellationToken);
        var isNewSignal = signal is null;
        var wasUncoveredOrDismissed = signal is null
            || signal.Coverage.IsUncovered
            || signal.Coverage == SignalCoverage.Dismissed;
        var hasBeenDismissed = signal?.Coverage == SignalCoverage.Dismissed;

        if (signal is null)
        {
            signal = new MachineSignal(message.EpisodeId, message.MachineId, message.CompanyId, change);
            _dbContext.MachineSignals.Add(signal);
        }
        else if (!signal.Apply(change))
        {
            return;
        }

        var caseOfSignal = await PlaceAsync(signal, isNewSignal, hasBeenDismissed, cancellationToken);

        var isResolved = false;

        if (signal.State == SignalState.Resolved && signal.CaseId is { } caseId)
        {
            var resolvedCase = await _dbContext.MaintenanceCases
                .Include(candidate => candidate.Signals)
                .SingleAsync(candidate => candidate.Id == caseId, cancellationToken);
            MarkCaseTouched(resolvedCase);
            isResolved = resolvedCase.ResolveIfDone(now);
        }

        if (signal.IsOpen && wasUncoveredOrDismissed && caseOfSignal is { } openCase)
        {
            await _runQueue.QueueTriageAsync(openCase.Id, now, cancellationToken);
        }

        var isNewCase = caseOfSignal is not null && _dbContext.Entry(caseOfSignal).State == EntityState.Added;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (isNewCase)
        {
            _notifier.AddToGroup(
                message.CompanyId,
                RealtimeNotificationKind.MaintenanceCaseOpened,
                new MaintenanceCaseOpenedData { CaseId = caseOfSignal!.Id, Severity = signal.Severity },
                isActorSkipped: false);
        }

        if (isNewCase || isResolved)
        {
            _notifier.AddCasesHint(message.CompanyId, signal.CaseId!.Value);
        }
    }

    private async Task<MaintenanceCase?> PlaceAsync(
        MachineSignal signal,
        bool isNewSignal,
        bool hasBeenDismissed,
        CancellationToken cancellationToken
    )
    {
        if (!signal.IsOpen)
        {
            return null;
        }

        var isEscalationOfDismissed = !isNewSignal && hasBeenDismissed;
        var needsCase = signal.CaseId is null || isEscalationOfDismissed;

        if (!needsCase)
        {
            return await _dbContext.MaintenanceCases.SingleOrDefaultAsync(
                candidate => candidate.Id == signal.CaseId && candidate.State == CaseState.Open,
                cancellationToken);
        }

        if (isEscalationOfDismissed)
        {
            signal.Reopen();
        }

        var openCase = await _dbContext.MaintenanceCases
            .Include(candidate => candidate.Signals)
            .SingleOrDefaultAsync(
                candidate => candidate.MachineId == signal.MachineId && candidate.State == CaseState.Open,
                cancellationToken);

        if (openCase is null)
        {
            openCase = new MaintenanceCase(signal.CompanyId, signal.MachineId, signal.OpenedAt);
            _dbContext.MaintenanceCases.Add(openCase);
            await TakeOverOpenSignalsAsync(openCase, signal, cancellationToken);
        }

        if (openCase.Signals.All(candidate => candidate.Id != signal.Id))
        {
            openCase.Add(signal);
            MarkCaseTouched(openCase);
        }

        return openCase;
    }

    /// <summary>
    /// Marks the case row as changed, so the save checks the row version of the case. A concurrent change of the
    /// same case (a join, a resolve) then fails one of the two saves, and the inbox retries the message. A new case
    /// has no stored row yet, so it needs no mark.
    /// </summary>
    /// <param name="maintenanceCase">The case that the message joins or resolves.</param>
    private void MarkCaseTouched(MaintenanceCase maintenanceCase)
    {
        var entry = _dbContext.Entry(maintenanceCase);

        if (entry.State == EntityState.Unchanged)
        {
            entry.Property(candidate => candidate.State).IsModified = true;
        }
    }

    private async Task TakeOverOpenSignalsAsync(
        MaintenanceCase newCase,
        MachineSignal current,
        CancellationToken cancellationToken
    )
    {
        var waiting = await _dbContext.MachineSignals
            .Where(candidate => candidate.MachineId == current.MachineId
                && candidate.CompanyId == current.CompanyId
                && candidate.State == SignalState.Open
                && candidate.Coverage != SignalCoverage.Dismissed
                && candidate.Id != current.Id)
            .ToListAsync(cancellationToken);

        foreach (var signal in waiting)
        {
            newCase.Add(signal);
        }
    }
}
