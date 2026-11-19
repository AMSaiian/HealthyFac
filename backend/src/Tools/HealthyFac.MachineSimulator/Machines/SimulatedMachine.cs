using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Machines.Snapshots;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// One machine of the simulator: its scenario, its modules, its counters, and the gates that decide whether it
/// publishes. The class holds no lock. <see cref="Simulation.SimulationEngine"/> calls it inside its lock.
/// </summary>
public sealed class SimulatedMachine
{
    private readonly TimeSpan _drainDuration;
    private readonly Random _random;
    private readonly Dictionary<string, double> _memory = [];
    private Dictionary<string, double> _counters = [];
    private IMachineScenario _scenario;
    private Dictionary<ModuleName, ModuleState> _modules = [];

    /// <summary>
    /// Creates a machine from its first snapshot.
    /// </summary>
    /// <param name="snapshot">The first snapshot.</param>
    /// <param name="discoveredAt">
    /// The UTC instant of the discovery. A maintenance that ended before it resets nothing.
    /// </param>
    /// <param name="drainDuration">The run time that a full drain needs.</param>
    /// <param name="random">The random source.</param>
    public SimulatedMachine(MachineSnapshot snapshot, DateTime discoveredAt, TimeSpan drainDuration, Random random)
    {
        Snapshot = snapshot;
        LastReset = discoveredAt;
        _drainDuration = drainDuration;
        _random = random;
        _scenario = ScenarioBinding.Create(ScenarioBinding.KindOf(snapshot.SerialNumber));
        Bind();
    }

    public MachineSnapshot Snapshot { get; private set; }

    /// <summary>
    /// The run flag. A machine that does not run publishes nothing. It starts as <see langword="true"/>.
    /// </summary>
    public bool IsRunning { get; private set; } = true;

    /// <summary>The UTC instant of the last reset. It starts at the discovery.</summary>
    public DateTime LastReset { get; private set; }

    public TimeSpan RunTime { get; private set; }

    public ScenarioKind ScenarioKind => _scenario.Kind;

    public IReadOnlyDictionary<ModuleName, ModuleState> Modules => _modules;

    public IReadOnlyDictionary<string, double> Counters => _counters;

    public IReadOnlyList<ModuleDefinition> ModuleDefinitions => _scenario.Modules;

    /// <summary>
    /// Takes a new snapshot. A new scenario binds again and resets the modules and the counters. A maintenance that
    /// ended after the last reset resets the modules.
    /// </summary>
    /// <param name="snapshot">The new snapshot.</param>
    /// <returns>The kinds of change that the snapshot caused, for the logs.</returns>
    public SnapshotChange Apply(MachineSnapshot snapshot)
    {
        Snapshot = snapshot;
        var change = SnapshotChange.None;

        var kind = ScenarioBinding.KindOf(snapshot.SerialNumber);
        if (kind != _scenario.Kind)
        {
            _scenario = ScenarioBinding.Create(kind);
            Bind();
            change |= SnapshotChange.Rebound;
        }

        var resetAt = FindMaintenanceReset(snapshot);
        if (resetAt is { } at)
        {
            foreach (var module in _modules.Values)
            {
                module.SetMode(ModuleMode.Expected);
            }

            LastReset = at;
            change |= SnapshotChange.Reset;
        }

        return change;
    }

    /// <summary>
    /// Sets the counters to the last stored readings. A key without a reading keeps its start value.
    /// </summary>
    /// <param name="readings">The latest stored value of each counter key.</param>
    public void ApplyCounterStart(IReadOnlyDictionary<string, double> readings)
    {
        foreach (var (key, value) in readings)
        {
            if (_counters.ContainsKey(key))
            {
                _counters[key] = value;
            }
        }
    }

    public bool IsLive(DateTime now) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, Snapshot.Zone)) >= Snapshot.ExpectedGoLiveOn;

    public bool IsPublishing(DateTime now) =>
        Snapshot.IsRegistered
        && IsLive(now)
        && IsRunning
        && (_scenario.Kind != ScenarioKind.Generic || Snapshot.CurrentState == MachineState.Operational);

    public void SetRunning(bool isRunning) => IsRunning = isRunning;

    /// <summary>
    /// Sets the mode of a module.
    /// </summary>
    /// <param name="name">The module name.</param>
    /// <param name="mode">The new mode.</param>
    /// <returns><see langword="false"/> when the machine has no such module.</returns>
    public bool TrySetMode(ModuleName name, ModuleMode mode)
    {
        if (!_modules.TryGetValue(name, out var module))
        {
            return false;
        }

        module.SetMode(mode);

        return true;
    }

    /// <summary>
    /// Makes the next message. A machine that does not publish makes none and changes nothing. A running sample adds
    /// the elapsed time to the run time, the growing counters, and the drain of each draining module.
    /// </summary>
    /// <param name="now">The UTC instant of the tick.</param>
    /// <param name="elapsed">The time since the last tick of this machine.</param>
    /// <returns>The sample and the crossed milestones, or <see langword="null"/> without a message.</returns>
    public TickResult? Tick(DateTime now, TimeSpan elapsed)
    {
        if (!IsPublishing(now))
        {
            return null;
        }

        var sample = _scenario.Sample(
            new ScenarioContext
            {
                RunTime = RunTime,
                Elapsed = elapsed,
                Modules = _modules,
                Counters = _counters,
                IsStopped = Snapshot.CurrentState != MachineState.Operational,
                Random = _random,
                Memory = _memory,
                Definitions = Snapshot.Definitions,
            });

        List<(ModuleName, DrainMilestone)> milestones = [];
        if (sample.IsRunning)
        {
            RunTime += elapsed;
            foreach (var key in sample.GrowingCounters.Where(_counters.ContainsKey))
            {
                _counters[key] += elapsed.TotalHours;
            }

            foreach (var (name, module) in _modules)
            {
                milestones.AddRange(module.Advance(elapsed, _drainDuration).Select(milestone => (name, milestone)));
            }
        }

        return new TickResult { Sample = sample, Milestones = milestones };
    }

    private void Bind()
    {
        _modules = _scenario.Modules.ToDictionary(module => module.Name, module => new ModuleState(module.Kind));
        _counters = new Dictionary<string, double>(_scenario.CounterStartValues);
        _memory.Clear();
        RunTime = TimeSpan.Zero;
    }

    private DateTime? FindMaintenanceReset(MachineSnapshot snapshot)
    {
        var rows = snapshot.StateHistory.OrderBy(entry => entry.StartedAt).ToList();
        DateTime? resetAt = null;

        for (var index = 1; index < rows.Count; index++)
        {
            var isMaintenanceEnd = rows[index - 1].State == MachineState.InMaintenance
                && rows[index].State == MachineState.Operational;
            if (isMaintenanceEnd && rows[index].StartedAt > LastReset)
            {
                resetAt = rows[index].StartedAt;
            }
        }

        return resetAt;
    }
}
