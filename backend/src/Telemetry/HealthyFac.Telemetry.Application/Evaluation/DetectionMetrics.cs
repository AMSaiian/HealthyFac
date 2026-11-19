using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using Results = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Evaluation.Results;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Evaluation.Tags;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Evaluation.Instruments;
using Units = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Units;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Holds the instruments of the <see cref="ObservabilityConstants.Evaluation.Meter"/> meter. Only the result of a run
/// is a tag, with a fixed set of values: a machine ID or a company ID would raise the cardinality of the series.
/// </summary>
public sealed class DetectionMetrics
{
    private readonly Counter<long> _lateReadings;
    private readonly Counter<long> _fleetSilences;
    private readonly Counter<long> _runs;
    private readonly Histogram<double> _runDuration;

    private volatile int _dueMachines;

    /// <summary>
    /// Creates the meter through <paramref name="meterFactory"/>, so each service provider owns its own meter and a
    /// test reads only the measurements of its own host.
    /// </summary>
    /// <param name="meterFactory">The meter factory of the host.</param>
    public DetectionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ObservabilityConstants.Evaluation.Meter);
        _lateReadings = meter.CreateCounter<long>(Instruments.LateReadings, Units.Readings);
        _fleetSilences = meter.CreateCounter<long>(Instruments.FleetSilences, Units.Silences);
        _runs = meter.CreateCounter<long>(Instruments.Runs, Units.Runs);
        _runDuration = meter.CreateHistogram<double>(Instruments.RunDuration, Units.Seconds);
        meter.CreateObservableGauge(Instruments.DueMachines, () => _dueMachines, Units.Machines);
    }

    /// <summary>
    /// Counts the readings that arrived after the watermark had passed their sampling time.
    /// </summary>
    /// <param name="count">The number of late readings of one run. A count of 0 records nothing.</param>
    public void AddLateReadings(long count)
    {
        if (count > 0)
        {
            _lateReadings.Add(count);
        }
    }

    /// <summary>
    /// Counts one finished machine run and records its duration. A cancelled run is neither completed nor failed,
    /// so the caller never records it.
    /// </summary>
    /// <param name="isCompleted"><see langword="true"/> for a completed run, <see langword="false"/> for a failed
    /// run.</param>
    /// <param name="elapsed">The time of the run.</param>
    public void RecordRun(bool isCompleted, TimeSpan elapsed)
    {
        var result = new KeyValuePair<string, object?>(
            Tags.Result,
            isCompleted ? Results.Completed : Results.Failed);

        _runs.Add(delta: 1, result);
        _runDuration.Record(elapsed.TotalSeconds, result);
    }

    /// <summary>
    /// Stores the number of due machines that the worker found at the start of its last round. The gauge reads the
    /// stored value, so the metric export never waits for the database.
    /// </summary>
    /// <param name="count">The number of machines with a due time that is not after now.</param>
    public void UpdateDueMachines(int count) => _dueMachines = count;

    /// <summary>
    /// Counts one change of the fleet from sending to silent.
    /// </summary>
    public void AddFleetSilence() => _fleetSilences.Add(delta: 1);
}
