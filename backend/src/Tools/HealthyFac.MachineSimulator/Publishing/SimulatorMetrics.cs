using System.Diagnostics.Metrics;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Shared.Application.Observability;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Simulator.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Simulator.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Holds the instruments of the <see cref="ObservabilityConstants.Simulator.Meter"/> meter. The tags have a small,
/// fixed set of values: never a gateway ID or a machine ID.
/// </summary>
public sealed class SimulatorMetrics
{
    private readonly Counter<long> _messages;
    private readonly Histogram<double> _pollDuration;
    private Func<int> _readPublishing = () => 0;

    /// <summary>
    /// Creates the meter through <paramref name="meterFactory"/>, so a test reads only the measurements of its own
    /// host.
    /// </summary>
    /// <param name="meterFactory">The meter factory of the host.</param>
    public SimulatorMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ObservabilityConstants.Simulator.Meter);
        _messages = meter.CreateCounter<long>(Instruments.Messages, Units.Messages);
        _pollDuration = meter.CreateHistogram<double>(Instruments.PollDuration, Units.Seconds);
        meter.CreateObservableGauge(Instruments.Publishing, () => _readPublishing(), Units.Machines);
    }

    /// <summary>
    /// Sets the source of the gauge of the machines that publish.
    /// </summary>
    /// <param name="read">Reads the count.</param>
    public void SetPublishingSource(Func<int> read) => _readPublishing = read;

    /// <summary>
    /// Counts one message.
    /// </summary>
    /// <param name="scenario">The scenario of the machine.</param>
    /// <param name="isPublished"><see langword="true"/> when the broker took the message, otherwise dropped.</param>
    public void RecordMessage(ScenarioKind scenario, bool isPublished) =>
        _messages.Add(
            delta: 1,
            new KeyValuePair<string, object?>(Tags.Scenario, scenario.Value),
            new KeyValuePair<string, object?>(
                Tags.Outcome,
                isPublished
                    ? ObservabilityConstants.Simulator.Outcomes.Published
                    : ObservabilityConstants.Simulator.Outcomes.Dropped));

    /// <summary>
    /// Records the duration of a poll.
    /// </summary>
    /// <param name="poll">The poll, from <see cref="ObservabilityConstants.Simulator.Polls"/>.</param>
    /// <param name="elapsed">The duration of the poll.</param>
    public void RecordPoll(string poll, TimeSpan elapsed) =>
        _pollDuration.Record(elapsed.TotalSeconds, new KeyValuePair<string, object?>(Tags.Poll, poll));
}
