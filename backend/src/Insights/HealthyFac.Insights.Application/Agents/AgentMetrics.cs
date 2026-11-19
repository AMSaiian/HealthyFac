using System.Diagnostics.Metrics;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Application.Observability;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Agents.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Agents.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Insights.Application.Agents;

/// <summary>
/// The instruments of the agent worker (meter <c>HealthyFac.Insights.Agents</c>): the runs by role and result, the
/// duration of the finished runs, and a gauge of the queued runs that are due. The gauge callback reads only the
/// stored sample, so the metric export never waits for the database. No ID, prompt, or model output enters a tag.
/// </summary>
public sealed class AgentMetrics
{
    private readonly Counter<long> _runs;
    private readonly Histogram<double> _duration;

    private long? _queuedRuns;

    public AgentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ObservabilityConstants.Agents.Source);
        _runs = meter.CreateCounter<long>(Instruments.Runs, Units.Runs);
        _duration = meter.CreateHistogram<double>(Instruments.RunDuration, Units.Seconds);
        meter.CreateObservableGauge(Instruments.QueuedRuns, ObserveQueuedRuns, Units.Runs);
    }

    /// <summary>
    /// Counts one finished run and records its duration.
    /// </summary>
    /// <param name="role">The role of the run.</param>
    /// <param name="result">The final state, from <see cref="ObservabilityConstants.Agents.Results"/>.</param>
    /// <param name="duration">The time from the start to the end of the run.</param>
    public void RecordRun(AgentRole role, string result, TimeSpan duration)
    {
        var roleTag = new KeyValuePair<string, object?>(Tags.Role, role.Value);
        _runs.Add(delta: 1, roleTag, new KeyValuePair<string, object?>(Tags.Result, result));
        _duration.Record(Math.Max(duration.TotalSeconds, val2: 0), roleTag);
    }

    /// <summary>
    /// Counts a run that the sweep interrupted. The run has no end time of its own, so it has no duration value.
    /// </summary>
    /// <param name="role">The role of the run.</param>
    public void RecordInterrupted(AgentRole role) =>
        _runs.Add(
            delta: 1,
            new KeyValuePair<string, object?>(Tags.Role, role.Value),
            new KeyValuePair<string, object?>(Tags.Result, ObservabilityConstants.Agents.Results.Interrupted));

    /// <summary>
    /// Stores the last sample of the queued runs that are due. A <see langword="null"/> sample clears it.
    /// </summary>
    /// <param name="queuedRuns">The sample, or <see langword="null"/>.</param>
    public void UpdateQueuedRuns(long? queuedRuns) => _queuedRuns = queuedRuns;

    private IEnumerable<Measurement<long>> ObserveQueuedRuns()
    {
        var sample = _queuedRuns;

        return sample is null ? [] : [new Measurement<long>(sample.Value)];
    }
}
