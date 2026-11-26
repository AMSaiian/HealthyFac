using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;

namespace HealthyFac.Seeder.TelemetryHistory.Simulation;

/// <summary>
/// Simulates one machine of the seed from the window start on, in ticks of the sample interval, without a database.
/// The ticks lie on the window start and on whole multiples of the interval after it. The first tick uses one
/// interval as its elapsed time, like the first tick of the live engine. The machine counts as discovered at the
/// window start.
/// </summary>
public sealed class HistorySimulation
{
    private readonly MachineSnapshot _snapshot;
    private readonly TimeSpan _interval;
    private readonly IReadOnlyList<StateHistoryEntry> _stateChanges;
    private readonly HistoryEvents _events;
    private int _appliedChanges;
    private readonly DateTime _windowStart;
    private DateTime _nextTick;

    /// <summary>Creates the simulation.</summary>
    /// <param name="snapshot">The first snapshot of the machine, with the state rows before the window start.</param>
    /// <param name="windowStart">The UTC start of the window. The first tick lies on it.</param>
    /// <param name="interval">The time between two ticks.</param>
    /// <param name="random">The random source of the machine.</param>
    /// <param name="stateChanges">The state changes after the window start, ordered by time.</param>
    /// <param name="events">The events of the machine.</param>
    public HistorySimulation(
        MachineSnapshot snapshot,
        DateTime windowStart,
        TimeSpan interval,
        Random random,
        IReadOnlyList<StateHistoryEntry> stateChanges,
        HistoryEvents events
    )
    {
        _snapshot = snapshot;
        _interval = interval;
        _stateChanges = stateChanges;
        _events = events;
        _windowStart = windowStart;
        _nextTick = windowStart;
        Machine = new SimulatedMachine(snapshot, windowStart, TelemetryHistoryConstants.DrainDuration, random);
    }

    public SimulatedMachine Machine { get; }

    /// <summary>
    /// Ticks the machine up to a time and returns the samples after another time. A tick at or before
    /// <paramref name="fromExclusive"/> still moves the machine, so the next call continues from the last tick. The
    /// sample of the window start always counts, so the first call of a run may pass the window start as
    /// <paramref name="fromExclusive"/>.
    /// </summary>
    /// <param name="fromExclusive">The UTC time after which a sample counts.</param>
    /// <param name="toInclusive">The UTC time of the last tick.</param>
    /// <returns>The samples in time order. A gap, a machine in maintenance without a message, and a machine that
    /// does not publish give none.</returns>
    public IReadOnlyList<HistorySample> Run(DateTime fromExclusive, DateTime toInclusive)
    {
        var samples = new List<HistorySample>();

        while (_nextTick <= toInclusive)
        {
            var at = _nextTick;
            _nextTick += _interval;
            var sample = Advance(at);

            if (sample is not null && (at > fromExclusive || at == _windowStart))
            {
                samples.Add(new HistorySample { SampledAt = at, Sample = sample });
            }
        }

        return samples;
    }

    private ScenarioSample? Advance(DateTime at)
    {
        ApplyStateChanges(at);
        _events.StartDrains(Machine, at);
        var tick = Machine.Tick(at, _interval);

        if (tick is null || _events.IsGap(at))
        {
            return null;
        }

        return _events.Shape(tick.Sample, at);
    }

    private void ApplyStateChanges(DateTime at)
    {
        var appliedBefore = _appliedChanges;

        while (_appliedChanges < _stateChanges.Count && _stateChanges[_appliedChanges].StartedAt <= at)
        {
            _appliedChanges++;
        }

        if (_appliedChanges == appliedBefore)
        {
            return;
        }

        Machine.Apply(
            _snapshot with { StateHistory = [.. _snapshot.StateHistory, .. _stateChanges.Take(_appliedChanges)] });
    }
}
