using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.UnitTests.Evaluation.Data;

/// <summary>
/// Runs a series through the statistics and the drift and trend rules, as the evaluation does, and records what the
/// rules decided and when the conditions first held.
/// </summary>
public sealed class DriftPipeline
{
    private const double AlarmHigh = 100;

    public static readonly RuleSettings Rules = new()
    {
        Persistence = 2,
        DriftLimit = 3,
        DriftResolveLimit = 2,
        HorizonSeconds = 900,
        NoiseGuardFactor = 3,
        DriftDurationSeconds = 40,
        TrendDurationSeconds = 120,
    };

    private readonly RuleStreak _driftStreak = new(Guid.CreateVersion7(), SignalRule.Drift, Guid.CreateVersion7());
    private readonly RuleStreak _trendStreak = new(Guid.CreateVersion7(), SignalRule.Trend, Guid.CreateVersion7());
    private SignalSeverity? _openDrift;
    private SignalSeverity? _openTrend;

    public MetricStatistics Statistics { get; } = new(Guid.CreateVersion7(), operatingPhaseId: null);

    public List<(DateTime Time, RuleDecision Decision)> DriftDecisions { get; } = [];

    public List<(DateTime Time, RuleDecision Decision)> TrendDecisions { get; } = [];

    public DateTime? FirstDriftConditionAt { get; private set; }

    public DateTime? FirstTrendConditionAt { get; private set; }

    public void ForgetFirstConditions()
    {
        FirstDriftConditionAt = null;
        FirstTrendConditionAt = null;
    }

    public void Run(IEnumerable<(DateTime Time, double Value)> series, bool isFrozen = false)
    {
        foreach (var (time, value) in series)
        {
            Statistics.Apply(value, time, isFrozen, MetricBehavior.Level, SignalSeries.Settings);

            if (!Statistics.IsReady(SignalSeries.Settings))
            {
                continue;
            }

            if (Statistics.IsDriftReady(SignalSeries.Settings))
            {
                RunDrift(time, value);
            }

            RunTrend(time, value);
        }
    }

    private void RunDrift(DateTime time, double value)
    {
        var z = Statistics.Z(min: 0, max: 200, SignalSeries.Settings);
        var (condition, snapshot) = DriftRule.Evaluate(value, z, Rules);
        FirstDriftConditionAt ??= condition.IsOpen ? time : null;

        var decision = _driftStreak.Observe(
            condition,
            _openDrift,
            time,
            Rules.Persistence,
            Rules.Persistence,
            snapshot,
            TimeSpan.FromSeconds(Rules.DriftDurationSeconds));

        _openDrift = Next(decision, _openDrift);
        DriftDecisions.Add((time, decision));
    }

    private void RunTrend(DateTime time, double value)
    {
        var forecast = Statistics.ForecastSeconds(alarmLow: null, AlarmHigh);

        var (condition, snapshot) = TrendRule.Evaluate(
            value,
            forecast,
            Statistics.Slope,
            Statistics.Sigma(min: 0, max: 200, SignalSeries.Settings),
            MetricBehavior.Level,
            Rules);

        FirstTrendConditionAt ??= condition.IsOpen ? time : null;

        var decision = _trendStreak.Observe(
            condition,
            _openTrend,
            time,
            Rules.Persistence,
            Rules.Persistence,
            snapshot,
            TimeSpan.FromSeconds(Rules.TrendDurationSeconds));

        _openTrend = Next(decision, _openTrend);
        TrendDecisions.Add((time, decision));
    }

    private static SignalSeverity? Next(RuleDecision decision, SignalSeverity? current) =>
        decision.Kind switch
        {
            RuleDecisionKind.Open or RuleDecisionKind.Escalate => decision.Severity,
            RuleDecisionKind.Resolve => null,
            _ => current,
        };
}
