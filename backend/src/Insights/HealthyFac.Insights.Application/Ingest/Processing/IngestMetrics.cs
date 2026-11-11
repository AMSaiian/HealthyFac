using System.Diagnostics;
using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Ingest.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Ingest.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// The instruments of the ingest (meter <c>HealthyFac.Insights.Ingest</c>): the attempts by result, the duration of
/// each stage, the pages, chunks, and drafts of completed attempts, and two gauges of the queue. The gauge callbacks
/// read only the stored sample, so the metric export never waits for the database. No ID, file name, or text enters a
/// tag.
/// </summary>
public sealed class IngestMetrics
{
    private readonly TimeProvider _timeProvider;
    private readonly Counter<long> _attempts;
    private readonly Histogram<double> _duration;
    private readonly Counter<long> _pages;
    private readonly Counter<long> _chunks;
    private readonly Counter<long> _drafts;

    private volatile IngestBacklog? _backlog;

    public IngestMetrics(IMeterFactory meterFactory, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var meter = meterFactory.Create(ObservabilityConstants.Ingest.Source);
        _attempts = meter.CreateCounter<long>(Instruments.Attempts, Units.Attempts);
        _duration = meter.CreateHistogram<double>(Instruments.Duration, Units.Seconds);
        _pages = meter.CreateCounter<long>(Instruments.Pages, Units.Pages);
        _chunks = meter.CreateCounter<long>(Instruments.Chunks, Units.Chunks);
        _drafts = meter.CreateCounter<long>(Instruments.Drafts, Units.Drafts);
        meter.CreateObservableGauge(Instruments.Pending, ObservePending, Units.Attempts);
        meter.CreateObservableGauge(Instruments.OldestPendingAge, ObserveOldestPendingAge, Units.Seconds);
    }

    /// <summary>
    /// Counts one finished run with its result and the error code or interrupt reason, when it has one.
    /// </summary>
    /// <param name="result">The final state, from <see cref="ObservabilityConstants.Ingest.Results"/>.</param>
    /// <param name="errorCode">The error code of a failed run, or <see langword="null"/>.</param>
    /// <param name="interruptReason">The reason of an interrupted run, or <see langword="null"/>.</param>
    public void RecordAttempt(string result, string? errorCode, string? interruptReason)
    {
        var tags = new TagList { { Tags.Result, result } };
        if (errorCode is not null)
        {
            tags.Add(Tags.ErrorCode, errorCode);
        }

        if (interruptReason is not null)
        {
            tags.Add(Tags.InterruptReason, interruptReason);
        }

        _attempts.Add(delta: 1, tags);
    }

    /// <summary>
    /// Starts the measure of one stage. The returned object records the duration when it is disposed.
    /// </summary>
    /// <param name="stage">The stage, from <see cref="ObservabilityConstants.Ingest.Stages"/>.</param>
    /// <returns>The timer. The caller disposes it at the end of the stage.</returns>
    public IDisposable MeasureStage(string stage) => new StageTimer(_duration, stage);

    /// <summary>
    /// Counts the pages, the chunks, and the drafts of a completed run.
    /// </summary>
    /// <param name="pages">The number of pages.</param>
    /// <param name="chunks">The number of stored chunks.</param>
    /// <param name="machines">The number of machine drafts.</param>
    /// <param name="metrics">The number of metric drafts.</param>
    /// <param name="statuses">The number of status drafts.</param>
    public void RecordCompleted(int pages, int chunks, int machines, int metrics, int statuses)
    {
        _pages.Add(pages);
        _chunks.Add(chunks);
        _drafts.Add(machines, DraftKind(ObservabilityConstants.Ingest.DraftKinds.Machine));
        _drafts.Add(metrics, DraftKind(ObservabilityConstants.Ingest.DraftKinds.Metric));
        _drafts.Add(statuses, DraftKind(ObservabilityConstants.Ingest.DraftKinds.Status));
    }

    /// <summary>
    /// Stores the last sample of the queue. A <see langword="null"/> sample clears it, so the gauges report no stale
    /// queue while the database is down.
    /// </summary>
    /// <param name="backlog">The sample, or <see langword="null"/>.</param>
    public void UpdateBacklog(IngestBacklog? backlog) => _backlog = backlog;

    private static KeyValuePair<string, object?> DraftKind(string kind) => new(Tags.DraftKind, kind);

    private IEnumerable<Measurement<long>> ObservePending()
    {
        var backlog = _backlog;

        return backlog is null ? [] : [new Measurement<long>(backlog.Count)];
    }

    private IEnumerable<Measurement<double>> ObserveOldestPendingAge()
    {
        var backlog = _backlog;
        if (backlog is null)
        {
            return [];
        }

        var age = backlog.OldestQueuedAt is { } oldest
            ? (_timeProvider.GetUtcNow().UtcDateTime - oldest).TotalSeconds
            : 0;

        return [new Measurement<double>(Math.Max(age, val2: 0))];
    }

    private sealed class StageTimer(Histogram<double> histogram, string stage) : IDisposable
    {
        private readonly Histogram<double> _histogram = histogram;
        private readonly string _stage = stage;
        private readonly long _startedAt = Stopwatch.GetTimestamp();

        public void Dispose() =>
            _histogram.Record(
                Stopwatch.GetElapsedTime(_startedAt).TotalSeconds,
                new KeyValuePair<string, object?>(Tags.Stage, _stage));
    }
}
