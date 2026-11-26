using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Telemetry.Domain.Evaluation;
using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Holds the loaded state of one run and processes the samples in order. The phase intervals follow the current phase:
/// an interval closes and the next one opens at the sample where the phase switches, and the silence rule closes the
/// open interval at the last sample. One sample goes through these steps: the phase
/// match and its dwell, the effective limits (the default limits of the metric with the limit row of the matched
/// phase), the threshold rule, the learning with the drift and trend rules in a tracked phase, and the status rule.
/// After the whole batch, the silence rule runs once.
/// </summary>
internal sealed class MachineRun
{
    private readonly ITelemetryDbContext _dbContext;
    private readonly DetectionOptions _options;
    private readonly MachineEvaluation _evaluation;
    private readonly IReadOnlyList<OperatingPhase> _phases;
    private readonly Dictionary<(Guid MetricId, Guid? PhaseId), MetricStatistics> _statistics;
    private readonly Dictionary<(SignalRule Rule, Guid? SubjectId), RuleStreak> _streaks;
    private readonly EpisodeChanges _changes;
    private readonly StatisticsSettings _statisticsSettings;
    private readonly RuleSettings _ruleSettings;
    private readonly PhaseSettings _phaseSettings;
    private readonly Guid _machineId;
    private readonly PhaseIntervalLog _intervals;
    private DateTime? _lastSampleAt;

    private MachineRun(
        ITelemetryDbContext dbContext,
        DetectionOptions options,
        MachineEvaluation evaluation,
        Guid machineId,
        IReadOnlyList<MetricDefinition> metrics,
        IReadOnlyList<StatusDefinition> statuses,
        IReadOnlyList<OperatingPhase> phases,
        IEnumerable<MetricStatistics> statistics,
        IEnumerable<RuleStreak> streaks,
        PhaseInterval? openInterval,
        EpisodeChanges changes
    )
    {
        _dbContext = dbContext;
        _options = options;
        _evaluation = evaluation;
        _machineId = machineId;
        _intervals = new PhaseIntervalLog(machineId, openInterval);
        _lastSampleAt = evaluation.Watermark;
        Metrics = metrics;
        Statuses = statuses;
        _phases = phases;
        _statistics = statistics.ToDictionary(row => (row.MetricDefinitionId, row.OperatingPhaseId));
        _streaks = streaks.ToDictionary(streak => (streak.Rule, streak.SubjectId));
        _changes = changes;

        _statisticsSettings = new StatisticsSettings
        {
            WarmUpSamples = options.WarmUpSamples,
            SlowTauSeconds = options.SlowTauSeconds,
            FastTauSeconds = options.FastTauSeconds,
            LevelTauSeconds = options.LevelTauSeconds,
            TrendTauSeconds = options.TrendTauSeconds,
            GapSeconds = options.GapSeconds,
            SigmaFloorShare = options.SigmaFloorShare,
        };

        _ruleSettings = new RuleSettings
        {
            Persistence = options.Persistence,
            DriftLimit = options.DriftLimit,
            DriftResolveLimit = options.DriftResolveLimit,
            HorizonSeconds = options.HorizonSeconds,
            NoiseGuardFactor = options.NoiseGuardFactor,
            DriftDurationSeconds = options.Persistence * options.FastTauSeconds,
            TrendDurationSeconds = options.Persistence * options.TrendTauSeconds,
        };

        _phaseSettings = new PhaseSettings
        {
            PhaseDwellSeconds = options.PhaseDwellSeconds,
            SettleTimeSeconds = options.SettleTimeSeconds,
        };
    }

    public IReadOnlyList<MetricDefinition> Metrics { get; }

    public IReadOnlyList<StatusDefinition> Statuses { get; }

    public int DefinitionCount => Metrics.Count + Statuses.Count;

    private bool HasPhases => _phases.Count > 0;

    public static async Task<MachineRun> LoadAsync(
        ITelemetryDbContext dbContext,
        IOutbox outbox,
        DetectionOptions options,
        MachineRegistration registration,
        MachineEvaluation evaluation,
        CancellationToken cancellationToken
    )
    {
        var machineId = registration.Id;

        var metrics = await dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.MachineRegistrationId == machineId)
            .ToListAsync(cancellationToken);

        var statuses = await dbContext.StatusDefinitions
            .AsNoTracking()
            .Where(status => status.MachineRegistrationId == machineId)
            .ToListAsync(cancellationToken);

        var phases = await dbContext.OperatingPhases
            .AsNoTracking()
            .Include(phase => phase.Conditions)
            .Include(phase => phase.Limits)
            .Where(phase => phase.MachineRegistrationId == machineId)
            .OrderBy(phase => phase.Priority)
            .ToListAsync(cancellationToken);

        var metricIds = metrics.Select(metric => metric.Id).ToList();

        var statistics = await dbContext.MetricStatistics
            .Where(row => metricIds.Contains(row.MetricDefinitionId))
            .ToListAsync(cancellationToken);

        var streaks = await dbContext.RuleStreaks
            .Where(streak => streak.MachineRegistrationId == machineId)
            .ToListAsync(cancellationToken);

        var openInterval = await dbContext.PhaseIntervals
            .Where(interval => interval.MachineRegistrationId == machineId && interval.EndedAt == null)
            .SingleOrDefaultAsync(cancellationToken);

        var openEpisodes = await dbContext.SignalEpisodes
            .Where(episode => episode.MachineRegistrationId == machineId
                && episode.State == SignalEpisodeState.Open)
            .ToListAsync(cancellationToken);

        var changes = new EpisodeChanges(
            dbContext,
            outbox,
            machineId,
            registration.CompanyId,
            openEpisodes,
            metrics.ToDictionary(metric => metric.Id, metric => metric.Key),
            statuses.ToDictionary(status => status.Id, status => status.Code),
            phases.ToDictionary(phase => phase.Id, phase => phase.Code));

        return new MachineRun(
            dbContext,
            options,
            evaluation,
            machineId,
            metrics,
            statuses,
            phases,
            statistics,
            streaks,
            openInterval,
            changes);
    }

    public void Process(Sample sample)
    {
        var t = sample.SampledAt;
        _lastSampleAt = t;
        var tracking = _evaluation.PhaseTracking;
        var previousPhaseId = tracking.CurrentPhaseId;
        var isLearningOpen = true;
        Guid? matched = null;

        if (!HasPhases)
        {
            _intervals.Close(t);
        }

        if (HasPhases)
        {
            matched = PhaseMatcher.Match(_phases, sample.Values);
            var step = tracking.Advance(matched, t, _phaseSettings);
            isLearningOpen = step.IsLearningOpen;

            if (_intervals.Follow(tracking.CurrentPhaseId, step.IsSwitched, t) is { } opened)
            {
                _dbContext.PhaseIntervals.Add(opened);
            }

            if (step.IsSwitched)
            {
                DelayThresholdCounts(previousPhaseId, tracking.CurrentPhaseId, t);
            }

            FollowUnknownMode(matched, tracking, t);
        }

        var phase = HasPhases ? _phases.FirstOrDefault(candidate => candidate.Id == tracking.CurrentPhaseId) : null;
        isLearningOpen = isLearningOpen && (!HasPhases || phase is not null);
        var phaseId = phase?.Id;

        var candidate = HasPhases && matched is not null && matched != tracking.CurrentPhaseId
            ? _phases.FirstOrDefault(other => other.Id == matched)
            : null;

        foreach (var metric in Metrics.Where(metric => sample.Values.Numeric.ContainsKey(metric.Id)))
        {
            var value = sample.Values.Numeric[metric.Id];
            var limits = EffectiveLimits(metric, phase);

            var openLimits = candidate is null
                ? limits
                : ThresholdRule.Looser(limits, EffectiveLimits(metric, candidate));

            RunThreshold(metric, value, limits, openLimits, t, phaseId);

            if (isLearningOpen && (!HasPhases || phase!.IsTracked))
            {
                LearnAndRun(metric, value, limits, t, phaseId);
            }
        }

        RunStatuses(sample, t, phaseId);
    }

    public async Task RunSilenceAsync(
        MachineRegistration registration,
        DateTime now,
        SampleBatchReader reader,
        bool isFleetSilent,
        CancellationToken cancellationToken
    )
    {
        var open = _changes.FindOpen(SignalRule.SilentMachine, metricId: null, statusId: null);

        if (open is not null)
        {
            var firstAfter = await reader.FirstReceivedAfterAsync(_machineId, open.OpenedAt, cancellationToken);

            if (firstAfter is { } receivedAt)
            {
                _changes.Resolve(open, receivedAt, SignalSnapshot.Empty);
            }

            return;
        }

        if (isFleetSilent)
        {
            return;
        }

        var latest = await reader.LatestReceivedAtAsync(_machineId, cancellationToken);

        var since = latest is { } lastMessage && lastMessage > registration.GoLiveStartsAt
            ? lastMessage
            : registration.GoLiveStartsAt;

        var limit = TimeSpan.FromSeconds(_options.SilenceLimitSeconds);

        if (now < registration.GoLiveStartsAt || now - since <= limit)
        {
            return;
        }

        _intervals.Close(_lastSampleAt ?? since);

        _changes.Open(
            SignalRule.SilentMachine,
            metricId: null,
            statusId: null,
            SignalSeverity.Alarm,
            since + limit,
            phaseId: null,
            SignalSnapshot.Empty);
    }

    private static MetricLimits EffectiveLimits(MetricDefinition metric, OperatingPhase? phase) =>
        phase?.Limits.FirstOrDefault(row => row.MetricDefinitionId == metric.Id)?.Limits ?? metric.Limits;

    private void DelayThresholdCounts(Guid? oldPhaseId, Guid? newPhaseId, DateTime t)
    {
        var oldPhase = _phases.FirstOrDefault(candidate => candidate.Id == oldPhaseId);
        var newPhase = _phases.FirstOrDefault(candidate => candidate.Id == newPhaseId);

        foreach (var metric in Metrics.Where(metric =>
            EffectiveLimits(metric, oldPhase) != EffectiveLimits(metric, newPhase)))
        {
            GetStreak(SignalRule.Threshold, metric.Id)
                .CountFrom(t.AddSeconds(_options.SettleTimeSeconds));
        }
    }

    private void FollowUnknownMode(Guid? matched, PhaseTracking tracking, DateTime t)
    {
        var open = _changes.FindOpen(SignalRule.UnknownMode, metricId: null, statusId: null);

        if (matched is not null && open is not null)
        {
            _changes.Resolve(open, t, SignalSnapshot.Empty);
        }
        else if (matched is null
            && open is null
            && tracking.UnmatchedSince is { } since
            && (t - since).TotalSeconds > _options.UnknownModeDwellSeconds)
        {
            _changes.Open(
                SignalRule.UnknownMode,
                metricId: null,
                statusId: null,
                SignalSeverity.Warning,
                t,
                phaseId: null,
                SignalSnapshot.Empty);
        }
    }

    private void RunThreshold(
        MetricDefinition metric,
        double value,
        MetricLimits limits,
        MetricLimits openLimits,
        DateTime t,
        Guid? phaseId
    )
    {
        var (condition, snapshot) = ThresholdRule.Evaluate(value, limits, openLimits);

        Observe(
            SignalRule.Threshold, metric.Id, statusId: null, condition, snapshot, t, phaseId, _options.Persistence);
    }

    private void LearnAndRun(MetricDefinition metric, double value, MetricLimits limits, DateTime t, Guid? phaseId)
    {
        var row = GetStatistics(metric.Id, phaseId);

        var update = row.Apply(
            value,
            t,
            _changes.HasOpenMetricEpisode(metric.Id),
            metric.Behavior,
            _statisticsSettings);

        if (update.IsIgnored || !row.IsReady(_statisticsSettings))
        {
            return;
        }

        if (metric.Behavior == MetricBehavior.Level && row.IsDriftReady(_statisticsSettings))
        {
            var (driftCondition, driftSnapshot) = DriftRule.Evaluate(
                value, row.Z(metric.Min, metric.Max, _statisticsSettings), _ruleSettings);

            Observe(
                SignalRule.Drift, metric.Id, statusId: null, driftCondition, driftSnapshot, t, phaseId,
                _options.Persistence, TimeSpan.FromSeconds(_ruleSettings.DriftDurationSeconds));
        }

        var (trendCondition, trendSnapshot) = TrendRule.Evaluate(
            value,
            row.ForecastSeconds(limits.AlarmLow, limits.AlarmHigh),
            row.Slope,
            row.Sigma(metric.Min, metric.Max, _statisticsSettings),
            metric.Behavior,
            _ruleSettings);

        Observe(
            SignalRule.Trend, metric.Id, statusId: null, trendCondition, trendSnapshot, t, phaseId,
            _options.Persistence, TimeSpan.FromSeconds(_ruleSettings.TrendDurationSeconds));
    }

    private void RunStatuses(Sample sample, DateTime t, Guid? phaseId)
    {
        foreach (var status in Statuses)
        {
            var condition = StatusRule.Evaluate(status.Severity, sample.Values.ActiveStatusIds.Contains(status.Id));

            Observe(
                SignalRule.Status, metricId: null, status.Id, condition, SignalSnapshot.Empty, t, phaseId,
                triggerPersistence: 1);
        }
    }

    private void Observe(
        SignalRule rule,
        Guid? metricId,
        Guid? statusId,
        RuleCondition condition,
        SignalSnapshot snapshot,
        DateTime t,
        Guid? phaseId,
        int triggerPersistence,
        TimeSpan? minimumDuration = null
    )
    {
        var streak = GetStreak(rule, metricId ?? statusId);

        var decision = streak.Observe(
            condition,
            _changes.FindOpen(rule, metricId, statusId)?.Severity,
            t,
            triggerPersistence,
            _options.Persistence,
            snapshot,
            minimumDuration);

        _changes.Apply(decision, rule, metricId, statusId, t, phaseId);
    }

    private RuleStreak GetStreak(SignalRule rule, Guid? subjectId)
    {
        if (_streaks.TryGetValue((rule, subjectId), out var streak))
        {
            return streak;
        }

        streak = new RuleStreak(_machineId, rule, subjectId);
        _dbContext.RuleStreaks.Add(streak);
        _streaks[(rule, subjectId)] = streak;

        return streak;
    }

    private MetricStatistics GetStatistics(Guid metricId, Guid? phaseId)
    {
        if (_statistics.TryGetValue((metricId, phaseId), out var row))
        {
            return row;
        }

        row = new MetricStatistics(metricId, phaseId);
        _dbContext.MetricStatistics.Add(row);
        _statistics[(metricId, phaseId)] = row;

        return row;
    }
}
