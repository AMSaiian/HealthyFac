using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.BackgroundQueues;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Realtime.Instruments;
using QueueInstruments =
    HealthyFac.Shared.Application.Observability.ObservabilityConstants.BackgroundQueues.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Realtime.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Holds the push counter of the <see cref="ObservabilityConstants.Meters.Realtime"/> meter and the queue length
/// gauge of the <see cref="ObservabilityConstants.Meters.BackgroundQueues"/> meter. It also counts an item that the
/// queue drops. Every tag has a small, fixed set of values: never a user ID.
/// </summary>
/// <remarks>
/// The class reads the queue through the service provider when the gauge is observed. The queue needs this class as
/// its drop observer, so a constructor parameter would make a cycle. A host without the queue (the gateway) reports
/// no gauge value.
/// </remarks>
public sealed class RealtimeMetrics : IBackgroundItemDropObserver<RealtimePush>
{
    private readonly Counter<long> _pushes;
    private readonly Histogram<double> _pushDuration;
    private readonly IServiceProvider _provider;

    public RealtimeMetrics(IMeterFactory meterFactory, IServiceProvider provider)
    {
        _provider = provider;

        var meter = meterFactory.Create(ObservabilityConstants.Meters.Realtime);
        _pushes = meter.CreateCounter<long>(Instruments.Pushes, Units.Pushes);
        _pushDuration = meter.CreateHistogram<double>(Instruments.PushDuration, Units.Seconds);

        var queueMeter = meterFactory.Create(ObservabilityConstants.Meters.BackgroundQueues);
        queueMeter.CreateObservableGauge(QueueInstruments.Length, ObserveQueueLength, Units.Items);
    }

    /// <summary>
    /// Counts one handled push and records its duration, from the first attempt to the end of the item.
    /// </summary>
    /// <param name="target">The kind value, the topic value, or the session end value.</param>
    /// <param name="result">The result, from <see cref="ObservabilityConstants.Realtime.Results"/>.</param>
    /// <param name="elapsed">The duration of the item, retries included.</param>
    public void RecordPush(string target, string result, TimeSpan elapsed)
    {
        Count(target, result);

        _pushDuration.Record(
            elapsed.TotalSeconds,
            new KeyValuePair<string, object?>(Tags.Target, target),
            new KeyValuePair<string, object?>(Tags.Result, result));
    }

    public void OnDropped(RealtimePush item) =>
        Count(item.MetricTarget, ObservabilityConstants.Realtime.Results.Dropped);

    private void Count(string target, string result) =>
        _pushes.Add(
            delta: 1,
            new KeyValuePair<string, object?>(Tags.Target, target),
            new KeyValuePair<string, object?>(Tags.Result, result));

    private IEnumerable<Measurement<int>> ObserveQueueLength()
    {
        if (_provider.GetService(typeof(BackgroundQueue<RealtimePush>)) is not BackgroundQueue<RealtimePush> queue)
        {
            return [];
        }

        return
        [
            new Measurement<int>(
                queue.Count,
                new KeyValuePair<string, object?>(
                    ObservabilityConstants.BackgroundQueues.Tags.ItemType,
                    ObservabilityConstants.BackgroundQueues.ItemTypes.RealtimePush)),
        ];
    }
}
