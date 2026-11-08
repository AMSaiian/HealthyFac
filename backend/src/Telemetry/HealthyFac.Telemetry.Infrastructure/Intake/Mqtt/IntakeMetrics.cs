using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Intake.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Intake.Tags;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Holds the instruments of the <see cref="ObservabilityConstants.Intake.Meter"/> meter. The service records one
/// result for each message. The gauges read <see cref="MqttIntakeState"/>. Every tag has a small, fixed set of
/// values: never a gateway ID, a topic, or a machine ID.
/// </summary>
public sealed class IntakeMetrics
{
    private readonly Counter<long> _messages;
    private readonly Histogram<double> _handleDuration;
    private readonly Histogram<double> _lag;
    private readonly Counter<long> _orphanEmptied;
    private readonly Counter<long> _drainTimeouts;

    /// <summary>
    /// Creates the meter through <paramref name="meterFactory"/>, so each service provider owns its own meter and
    /// a test reads only the measurements of its own host.
    /// </summary>
    /// <param name="meterFactory">The meter factory of the host.</param>
    /// <param name="state">The source of the gauges.</param>
    public IntakeMetrics(IMeterFactory meterFactory, MqttIntakeState state)
    {
        var meter = meterFactory.Create(ObservabilityConstants.Intake.Meter);
        _messages = meter.CreateCounter<long>(Instruments.Messages, Units.Messages);
        _handleDuration = meter.CreateHistogram<double>(Instruments.HandleDuration, Units.Seconds);
        _lag = meter.CreateHistogram<double>(Instruments.Lag, Units.Seconds);
        _orphanEmptied = meter.CreateCounter<long>(Instruments.OrphanEmptied, Units.Messages);
        _drainTimeouts = meter.CreateCounter<long>(Instruments.DrainTimeouts, Units.Sessions);
        meter.CreateObservableGauge(Instruments.InFlight, () => state.InFlightCount, Units.Messages);
        meter.CreateObservableGauge(Instruments.ConsumersConnected, () => state.ConnectedCount, Units.Sessions);
    }

    /// <summary>
    /// Counts one message and records how long the service handled it.
    /// </summary>
    /// <param name="outcome">The result of the message.</param>
    /// <param name="elapsed">The duration from the start of the handling until the acknowledge.</param>
    public void RecordMessage(IntakeOutcome outcome, TimeSpan elapsed)
    {
        var resultTag = new KeyValuePair<string, object?>(Tags.Result, outcome.Result);

        if (outcome.ErrorCode is null)
        {
            _messages.Add(delta: 1, resultTag);
        }
        else
        {
            _messages.Add(delta: 1, resultTag, new KeyValuePair<string, object?>(Tags.ErrorCode, outcome.ErrorCode));
        }

        _handleDuration.Record(elapsed.TotalSeconds, resultTag);
    }

    /// <summary>
    /// Records the delay between the sampling and the receipt of a stored message.
    /// </summary>
    /// <param name="lag">The receipt time minus the sampling time. A negative value (a clock ahead within the
    /// allowed margin) counts as 0.</param>
    public void RecordLag(TimeSpan lag) => _lag.Record(Math.Max(val1: 0, lag.TotalSeconds));

    public void RecordOrphanEmptied(int messageCount) => _orphanEmptied.Add(messageCount);

    public void RecordDrainTimeout() => _drainTimeouts.Add(delta: 1);
}
