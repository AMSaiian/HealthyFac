using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.Messaging.Signals;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Applies the decisions of the rules to the signal episodes of one machine and queues one
/// <see cref="SignalEpisodeChanged"/> message for each open, escalate, and resolve. The caller saves, so the
/// episodes and their messages commit together.
/// </summary>
public sealed class EpisodeChanges
{
    private static readonly SignalRule[] MetricRules = [SignalRule.Threshold, SignalRule.Drift, SignalRule.Trend];

    private readonly ITelemetryDbContext _dbContext;
    private readonly IOutbox _outbox;
    private readonly Guid _machineId;
    private readonly Guid _companyId;
    private readonly List<SignalEpisode> _openEpisodes;
    private readonly IReadOnlyDictionary<Guid, string> _metricKeys;
    private readonly IReadOnlyDictionary<Guid, string> _statusCodes;
    private readonly IReadOnlyDictionary<Guid, string> _phaseCodes;

    /// <summary>
    /// Creates the changes of one run.
    /// </summary>
    /// <param name="dbContext">Receives the new episodes.</param>
    /// <param name="outbox">Receives the messages.</param>
    /// <param name="machineId">The machine.</param>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="openEpisodes">The open episodes of the machine, tracked.</param>
    /// <param name="metricKeys">The metric key by metric ID.</param>
    /// <param name="statusCodes">The status code by status ID.</param>
    /// <param name="phaseCodes">The phase code by phase ID.</param>
    public EpisodeChanges(
        ITelemetryDbContext dbContext,
        IOutbox outbox,
        Guid machineId,
        Guid companyId,
        IEnumerable<SignalEpisode> openEpisodes,
        IReadOnlyDictionary<Guid, string> metricKeys,
        IReadOnlyDictionary<Guid, string> statusCodes,
        IReadOnlyDictionary<Guid, string> phaseCodes
    )
    {
        _dbContext = dbContext;
        _outbox = outbox;
        _machineId = machineId;
        _companyId = companyId;
        _openEpisodes = openEpisodes.ToList();
        _metricKeys = metricKeys;
        _statusCodes = statusCodes;
        _phaseCodes = phaseCodes;
    }

    /// <summary>
    /// Finds the open episode of a rule and subject.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="metricId">The metric of a metric rule.</param>
    /// <param name="statusId">The status of the status rule.</param>
    /// <returns>The episode, or <see langword="null"/> when none is open.</returns>
    public SignalEpisode? FindOpen(SignalRule rule, Guid? metricId, Guid? statusId) =>
        _openEpisodes.SingleOrDefault(episode => episode.Rule == rule
            && episode.MetricDefinitionId == metricId
            && episode.StatusDefinitionId == statusId);

    /// <summary>
    /// Tells whether a threshold, drift, or trend episode of the metric is open. Such an episode freezes the
    /// baseline of the metric.
    /// </summary>
    /// <param name="metricId">The metric.</param>
    /// <returns><see langword="true"/> when one is open.</returns>
    public bool HasOpenMetricEpisode(Guid metricId) =>
        _openEpisodes.Any(episode => episode.MetricDefinitionId == metricId && MetricRules.Contains(episode.Rule));

    /// <summary>
    /// Applies the decision of a rule: opens, escalates, or resolves the episode of the subject.
    /// </summary>
    /// <param name="decision">The decision.</param>
    /// <param name="rule">The rule.</param>
    /// <param name="metricId">The metric of a metric rule.</param>
    /// <param name="statusId">The status of the status rule.</param>
    /// <param name="at">The <c>sampled_at</c> time of the sample.</param>
    /// <param name="phaseId">The phase of the sample, or <see langword="null"/>.</param>
    public void Apply(
        RuleDecision decision,
        SignalRule rule,
        Guid? metricId,
        Guid? statusId,
        DateTime at,
        Guid? phaseId
    )
    {
        if (decision.Kind == RuleDecisionKind.None)
        {
            return;
        }

        var snapshot = decision.Snapshot with { PhaseCode = PhaseCode(phaseId) };
        var episode = FindOpen(rule, metricId, statusId);

        switch (decision.Kind)
        {
            case RuleDecisionKind.Open when episode is null:
                Open(rule, metricId, statusId, decision.Severity!, at, phaseId, snapshot);
                break;
            case RuleDecisionKind.Escalate when episode is not null:
                episode.Escalate(at, snapshot);
                Queue(episode, at);
                break;
            case RuleDecisionKind.Resolve when episode is not null:
                Resolve(episode, at, snapshot);
                break;
        }
    }

    /// <summary>
    /// Opens an episode and queues its message.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="metricId">The metric of a metric rule.</param>
    /// <param name="statusId">The status of the status rule.</param>
    /// <param name="severity">The severity at the open.</param>
    /// <param name="at">The time of the open.</param>
    /// <param name="phaseId">The phase of the sample, or <see langword="null"/>.</param>
    /// <param name="snapshot">The numbers of the open.</param>
    public void Open(
        SignalRule rule,
        Guid? metricId,
        Guid? statusId,
        SignalSeverity severity,
        DateTime at,
        Guid? phaseId,
        SignalSnapshot snapshot
    )
    {
        var episode = SignalEpisode.Open(
            _machineId,
            _companyId,
            rule,
            metricId,
            statusId,
            severity,
            at,
            phaseId,
            snapshot);

        _dbContext.SignalEpisodes.Add(episode);
        _openEpisodes.Add(episode);
        Queue(episode, at);
    }

    /// <summary>
    /// Resolves an open episode and queues its message.
    /// </summary>
    /// <param name="episode">The open episode.</param>
    /// <param name="at">The time of the resolve.</param>
    /// <param name="snapshot">The numbers of the resolve.</param>
    public void Resolve(SignalEpisode episode, DateTime at, SignalSnapshot snapshot)
    {
        episode.Resolve(at, snapshot);
        _openEpisodes.Remove(episode);
        Queue(episode, at);
    }

    /// <summary>
    /// Resolves every open episode of the machine.
    /// </summary>
    /// <param name="at">The time of the resolve.</param>
    public void ResolveAll(DateTime at)
    {
        foreach (var episode in _openEpisodes.ToList())
        {
            Resolve(episode, at, SignalSnapshot.Empty);
        }
    }

    private string? PhaseCode(Guid? phaseId) =>
        phaseId is { } id && _phaseCodes.TryGetValue(id, out var code) ? code : null;

    private void Queue(SignalEpisode episode, DateTime at)
    {
        _outbox.Add(
            new SignalEpisodeChanged
            {
                EpisodeId = episode.Id,
                MachineId = _machineId,
                CompanyId = _companyId,
                Rule = SignalRuleValue.FromValue(episode.Rule.Value),
                Severity = SignalSeverityValue.FromValue(episode.Severity.Value),
                State = SignalEpisodeStateValue.FromValue(episode.State.Value),
                MetricDefinitionId = episode.MetricDefinitionId,
                MetricKey = Lookup(_metricKeys, episode.MetricDefinitionId),
                StatusDefinitionId = episode.StatusDefinitionId,
                StatusCode = Lookup(_statusCodes, episode.StatusDefinitionId),
                PhaseCode = episode.Snapshot.PhaseCode,
                Value = episode.Snapshot.Value,
                Limit = episode.Snapshot.Limit,
                Z = episode.Snapshot.Z,
                ForecastSeconds = episode.Snapshot.ForecastSeconds,
                OpenedAt = episode.OpenedAt,
                ResolvedAt = episode.ResolvedAt,
                Sequence = episode.Sequence,
                ChangedAt = at,
            });
    }

    private static string? Lookup(IReadOnlyDictionary<Guid, string> map, Guid? id) =>
        id is { } key && map.TryGetValue(key, out var value) ? value : null;
}
