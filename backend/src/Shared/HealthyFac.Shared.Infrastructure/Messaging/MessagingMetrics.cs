using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Backlog;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Instruments;
using Processors = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Processors;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Holds the instruments of the <see cref="ObservabilityConstants.Meters.Messaging"/> meter. The processors record
/// the counters and the histograms. The backlog gauges report the last sample of
/// <see cref="MessageBacklogSampler"/>, so a metric export never queries the database. Every tag has a small,
/// fixed set of values: never a message ID.
/// </summary>
internal sealed class MessagingMetrics
{
    private readonly TimeProvider _timeProvider;
    private readonly Counter<long> _published;
    private readonly Counter<long> _handled;
    private readonly Counter<long> _retried;
    private readonly Counter<long> _failed;
    private readonly Histogram<double> _runDuration;
    private readonly Histogram<double> _handleDuration;

    private volatile MessageBacklog? _backlog;

    /// <summary>
    /// Creates the meter through <paramref name="meterFactory"/>, so each service provider owns its own meter and
    /// a test reads only the measurements of its own host.
    /// </summary>
    /// <param name="meterFactory">The meter factory of the host.</param>
    /// <param name="timeProvider">The clock for the age of the oldest pending row.</param>
    public MessagingMetrics(IMeterFactory meterFactory, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        var meter = meterFactory.Create(ObservabilityConstants.Meters.Messaging);
        _published = meter.CreateCounter<long>(Instruments.Published, Units.Messages);
        _handled = meter.CreateCounter<long>(Instruments.Handled, Units.Messages);
        _retried = meter.CreateCounter<long>(Instruments.Retried, Units.Messages);
        _failed = meter.CreateCounter<long>(Instruments.Failed, Units.Messages);
        _runDuration = meter.CreateHistogram<double>(Instruments.RunDuration, Units.Seconds);
        _handleDuration = meter.CreateHistogram<double>(Instruments.HandleDuration, Units.Seconds);
        meter.CreateObservableGauge(Instruments.Pending, ObservePending, Units.Messages);
        meter.CreateObservableGauge(Instruments.OldestPendingAge, ObserveOldestPendingAge, Units.Seconds);
    }

    public void RecordPublished(string messageType) =>
        _published.Add(delta: 1, ProcessorTag(Processors.Outbox), MessageTypeTag(messageType));

    public void RecordHandled(string messageType) =>
        _handled.Add(delta: 1, ProcessorTag(Processors.Inbox), MessageTypeTag(messageType));

    /// <summary>
    /// Counts a failed attempt that leaves the message pending for a later attempt.
    /// </summary>
    /// <param name="processor">The processor, from <see cref="Processors"/>.</param>
    /// <param name="messageType">The stable contract name of the message.</param>
    public void RecordRetried(string processor, string messageType) =>
        _retried.Add(delta: 1, ProcessorTag(processor), MessageTypeTag(messageType));

    /// <summary>
    /// Counts an inbox message that gets the failed state, so no processor claims it again.
    /// </summary>
    /// <param name="messageType">The stable contract name of the message.</param>
    /// <param name="failure">The reason, from <see cref="ObservabilityConstants.Messaging.Failures"/>.</param>
    public void RecordFailed(string messageType, string failure) =>
        _failed.Add(
            delta: 1,
            ProcessorTag(Processors.Inbox),
            MessageTypeTag(messageType),
            new KeyValuePair<string, object?>(Tags.Failure, failure));

    /// <summary>
    /// Records the duration of one processor run, from the first claim until no row is due.
    /// </summary>
    /// <param name="processor">The processor, from <see cref="Processors"/>.</param>
    /// <param name="outcome">The outcome, from <see cref="ObservabilityConstants.Outcomes"/>.</param>
    /// <param name="elapsed">The duration of the run.</param>
    public void RecordRun(string processor, string outcome, TimeSpan elapsed) =>
        _runDuration.Record(elapsed.TotalSeconds, ProcessorTag(processor), OutcomeTag(outcome));

    /// <summary>
    /// Records the duration of one publish (outbox) or one handling attempt (inbox).
    /// </summary>
    /// <param name="processor">The processor, from <see cref="Processors"/>.</param>
    /// <param name="messageType">The stable contract name of the message.</param>
    /// <param name="outcome">The outcome, from <see cref="ObservabilityConstants.Outcomes"/>.</param>
    /// <param name="elapsed">The duration of the attempt.</param>
    public void RecordHandle(string processor, string messageType, string outcome, TimeSpan elapsed) =>
        _handleDuration.Record(
            elapsed.TotalSeconds,
            ProcessorTag(processor),
            MessageTypeTag(messageType),
            OutcomeTag(outcome));

    /// <summary>
    /// Stores the last backlog sample, or clears it when the sample failed, so the gauges report no stale value.
    /// </summary>
    /// <param name="backlog">The new sample, or <see langword="null"/> after a failed sample.</param>
    public void UpdateBacklog(MessageBacklog? backlog) => _backlog = backlog;

    private IEnumerable<Measurement<long>> ObservePending()
    {
        var backlog = _backlog;
        if (backlog is null)
        {
            return [];
        }

        return
        [
            new Measurement<long>(backlog.Outbox.Count, ProcessorTag(Processors.Outbox)),
            new Measurement<long>(backlog.Inbox.Count, ProcessorTag(Processors.Inbox)),
        ];
    }

    /// <summary>
    /// Reports the age of the oldest pending row at the time of the export. The age is 0 when no row is pending.
    /// </summary>
    /// <returns>One measurement per processor, or none before the first sample.</returns>
    private IEnumerable<Measurement<double>> ObserveOldestPendingAge()
    {
        var backlog = _backlog;
        if (backlog is null)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return
        [
            new Measurement<double>(AgeInSeconds(backlog.Outbox, now), ProcessorTag(Processors.Outbox)),
            new Measurement<double>(AgeInSeconds(backlog.Inbox, now), ProcessorTag(Processors.Inbox)),
        ];
    }

    private static double AgeInSeconds(PendingBacklog backlog, DateTime now) =>
        backlog.OldestOn is null ? 0 : Math.Max(val1: 0, (now - backlog.OldestOn.Value).TotalSeconds);

    private static KeyValuePair<string, object?> ProcessorTag(string processor) => new(Tags.Processor, processor);

    private static KeyValuePair<string, object?> MessageTypeTag(string messageType) =>
        new(Tags.MessageType, messageType);

    private static KeyValuePair<string, object?> OutcomeTag(string outcome) =>
        new(ObservabilityConstants.Tags.Outcome, outcome);
}
