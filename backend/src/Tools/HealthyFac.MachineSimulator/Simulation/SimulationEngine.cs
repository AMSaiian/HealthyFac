using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Scenarios;
using Microsoft.Extensions.Options;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// Holds the simulated machines and changes them under one lock. A poll reads first and applies after, and a tick
/// builds its messages inside the lock and publishes them after. No call inside the lock does I/O or waits.
/// </summary>
/// <param name="options">The timing of the simulator.</param>
/// <param name="random">The random source of the scenarios.</param>
/// <param name="logger">Logs the events of the machines.</param>
public sealed class SimulationEngine(
    IOptions<SimulatorOptions> options,
    Random random,
    ILogger<SimulationEngine> logger
)
{
    private readonly SimulatorOptions _options = options.Value;
    private readonly Random _random = random;
    private readonly ILogger<SimulationEngine> _logger = logger;
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, SimulatedMachine> _machines = [];
    private readonly Dictionary<Guid, bool> _gates = [];
    private readonly Dictionary<Guid, bool> _stopsByTrip = [];
    private readonly HashSet<(Guid MachineId, string Key)> _warnedKeys = [];
    private DateTime? _lastTickAt;

    /// <summary>
    /// Lists the machines that the engine holds, so a discovery reads the counters of the new ones first.
    /// </summary>
    /// <returns>The machine IDs.</returns>
    public IReadOnlyCollection<Guid> KnownMachineIds()
    {
        lock (_gate)
        {
            return _machines.Keys.ToList();
        }
    }

    /// <summary>
    /// Adds the new machines, removes the machines that are gone, and applies the snapshots to the others.
    /// </summary>
    /// <param name="snapshots">The snapshots of all machines.</param>
    /// <param name="counterStarts">The stored counter values of the new machines, by machine ID.</param>
    /// <param name="now">The UTC instant of the discovery.</param>
    public void ApplyDiscovery(
        IReadOnlyList<MachineSnapshot> snapshots,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, double>> counterStarts,
        DateTime now
    )
    {
        lock (_gate)
        {
            RemoveMissing(snapshots);

            foreach (var snapshot in snapshots)
            {
                if (_machines.TryGetValue(snapshot.MachineId, out var known))
                {
                    LogChange(known, known.Apply(snapshot));

                    continue;
                }

                var machine = new SimulatedMachine(snapshot, now, _options.DrainDuration, _random);
                if (counterStarts.TryGetValue(snapshot.MachineId, out var readings))
                {
                    machine.ApplyCounterStart(readings);
                }

                _machines[snapshot.MachineId] = machine;
                _logger.LogInformation(
                    message:
                    "Found the machine {MachineId} with the scenario {Scenario}; the counters start at {Counters}",
                    snapshot.MachineId,
                    machine.ScenarioKind.Value,
                    machine.Counters);
            }
        }
    }

    /// <summary>
    /// Applies the snapshots to the known machines. A machine that the poll no longer returns (deleted, or a company
    /// without a valid zone) is removed, so it stops within one poll. A new machine waits for the discovery.
    /// </summary>
    /// <param name="snapshots">The snapshots of all machines.</param>
    /// <param name="now">The UTC instant of the poll.</param>
    public void ApplyStatePoll(IReadOnlyList<MachineSnapshot> snapshots, DateTime now)
    {
        lock (_gate)
        {
            RemoveMissing(snapshots);

            foreach (var snapshot in snapshots)
            {
                if (_machines.TryGetValue(snapshot.MachineId, out var machine))
                {
                    LogChange(machine, machine.Apply(snapshot));
                    LogGate(machine, now);
                }
            }
        }
    }

    /// <summary>
    /// Makes one tick: each publishing machine samples its scenario and grows its counters and drains. The elapsed time
    /// is the time since the last tick, and the first tick uses the publish interval.
    /// </summary>
    /// <param name="now">The UTC instant of the tick. It is the <c>sampledAt</c> of every message.</param>
    /// <returns>The messages to publish, after the lock is released.</returns>
    public IReadOnlyList<PendingMessage> BuildTick(DateTime now)
    {
        lock (_gate)
        {
            var elapsed = _lastTickAt is { } last ? now - last : _options.PublishInterval;
            _lastTickAt = now;
            List<PendingMessage> messages = [];

            foreach (var machine in _machines.Values)
            {
                LogGate(machine, now);
                var result = machine.Tick(now, elapsed);
                if (result is null)
                {
                    continue;
                }

                LogTick(machine, result);
                var payload = MachinePayloadBuilder.Build(result.Sample, machine.Snapshot.Definitions, now);
                WarnSkipped(machine, payload);
                messages.Add(
                    new PendingMessage
                    {
                        GatewayId = machine.Snapshot.GatewayId, Scenario = machine.ScenarioKind, Payload = payload.Json,
                        Keys = result.Sample.Values.Keys.ToList(), Codes = result.Sample.StatusCodes,
                    });
            }

            return messages;
        }
    }

    /// <summary>
    /// Counts the machines that publish at an instant, for the gauge.
    /// </summary>
    /// <param name="now">The UTC instant.</param>
    /// <returns>The number of publishing machines.</returns>
    public int PublishingCount(DateTime now)
    {
        lock (_gate)
        {
            return _machines.Values.Count(machine => machine.IsPublishing(now));
        }
    }

    public ControlResult SetRunning(Guid machineId, bool isRunning)
    {
        lock (_gate)
        {
            if (!_machines.TryGetValue(machineId, out var machine))
            {
                return ControlResult.MachineNotFound;
            }

            machine.SetRunning(isRunning);
            _logger.LogInformation(
                message: "The machine {MachineId} is now {Run}", machineId, isRunning ? "running" : "stopped");

            return ControlResult.Done;
        }
    }

    public ControlResult SetMode(Guid machineId, ModuleName module, ModuleMode mode)
    {
        lock (_gate)
        {
            if (!_machines.TryGetValue(machineId, out var machine))
            {
                return ControlResult.MachineNotFound;
            }

            if (!machine.TrySetMode(module, mode))
            {
                return ControlResult.ModuleNotFound;
            }

            _logger.LogInformation(
                message: "The module {Module} of the machine {MachineId} is now {Mode}",
                module.Value,
                machineId,
                mode.Value);

            return ControlResult.Done;
        }
    }

    /// <summary>
    /// Copies the state of every machine, ordered by company ID and then by name.
    /// </summary>
    /// <param name="now">The UTC instant, for the gates.</param>
    /// <returns>The views.</returns>
    public IReadOnlyList<MachineView> Snapshot(DateTime now)
    {
        lock (_gate)
        {
            return _machines.Values
                .Select(machine => ToView(machine, now))
                .OrderBy(view => view.CompanyId)
                .ThenBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public MachineView? Find(Guid machineId, DateTime now)
    {
        lock (_gate)
        {
            return _machines.TryGetValue(machineId, out var machine) ? ToView(machine, now) : null;
        }
    }

    private static MachineView ToView(SimulatedMachine machine, DateTime now) =>
        new()
        {
            MachineId = machine.Snapshot.MachineId,
            CompanyId = machine.Snapshot.CompanyId,
            Name = machine.Snapshot.Name,
            SerialNumber = machine.Snapshot.SerialNumber,
            GatewayId = machine.Snapshot.GatewayId,
            Scenario = machine.ScenarioKind,
            CoreState = machine.Snapshot.CurrentState,
            ExpectedGoLiveOn = machine.Snapshot.ExpectedGoLiveOn,
            IsRegistered = machine.Snapshot.IsRegistered,
            IsLive = machine.IsLive(now),
            IsRunning = machine.IsRunning,
            IsPublishing = machine.IsPublishing(now),
            Counters = new Dictionary<string, double>(machine.Counters),
            Modules = machine.ModuleDefinitions
                .Select(definition => new ModuleView
                {
                    Module = definition.Name,
                    Kind = definition.Kind,
                    Mode = machine.Modules[definition.Name].Mode,
                    DrainProgress = machine.Modules[definition.Name].Progress,
                })
                .ToList(),
        };

    private void RemoveMissing(IReadOnlyList<MachineSnapshot> snapshots)
    {
        var present = snapshots.Select(snapshot => snapshot.MachineId).ToHashSet();

        foreach (var machineId in _machines.Keys.Where(id => !present.Contains(id)).ToList())
        {
            _machines.Remove(machineId);
            _gates.Remove(machineId);
            _stopsByTrip.Remove(machineId);
            _warnedKeys.RemoveWhere(item => item.MachineId == machineId);
            _logger.LogInformation(message: "Removed the machine {MachineId}", machineId);
        }
    }

    private void LogChange(SimulatedMachine machine, SnapshotChange change)
    {
        if (change.HasFlag(SnapshotChange.Rebound))
        {
            _logger.LogInformation(
                message:
                "The machine {MachineId} has the new scenario {Scenario}; its modules and counters start again",
                machine.Snapshot.MachineId,
                machine.ScenarioKind.Value);
        }

        if (change.HasFlag(SnapshotChange.Reset))
        {
            _logger.LogInformation(
                message: "The machine {MachineId} left the maintenance state; every module is expected again",
                machine.Snapshot.MachineId);
        }
    }

    private void LogGate(SimulatedMachine machine, DateTime now)
    {
        var isPublishing = machine.IsPublishing(now);
        if (_gates.TryGetValue(machine.Snapshot.MachineId, out var wasPublishing) && wasPublishing == isPublishing)
        {
            return;
        }

        _gates[machine.Snapshot.MachineId] = isPublishing;
        _logger.LogInformation(
            message: "The machine {MachineId} {Gate} (registered {IsRegistered}, live {IsLive})",
            machine.Snapshot.MachineId,
            isPublishing ? "starts to publish" : "does not publish",
            machine.Snapshot.IsRegistered,
            machine.IsLive(now));
    }

    private void LogTick(SimulatedMachine machine, TickResult result)
    {
        foreach (var (module, milestone) in result.Milestones)
        {
            if (milestone == DrainMilestone.Trip)
            {
                _logger.LogInformation(
                    message: "The module {Module} of the machine {MachineId} failed after its drain",
                    module.Value,
                    machine.Snapshot.MachineId);
            }
            else
            {
                _logger.LogInformation(
                    message: "The module {Module} of the machine {MachineId} passed the {Milestone}",
                    module.Value,
                    machine.Snapshot.MachineId,
                    milestone);
            }
        }

        var isTripStop = !result.Sample.IsRunning && machine.Snapshot.CurrentState == MachineState.Operational;
        var wasTripStop = _stopsByTrip.GetValueOrDefault(machine.Snapshot.MachineId);
        _stopsByTrip[machine.Snapshot.MachineId] = isTripStop;
        if (isTripStop && !wasTripStop)
        {
            _logger.LogInformation(
                message: "The machine {MachineId} stopped because of a trip failure",
                machine.Snapshot.MachineId);
        }
    }

    private void WarnSkipped(SimulatedMachine machine, BuiltPayload payload)
    {
        foreach (var key in payload.SkippedKeys)
        {
            if (_warnedKeys.Add((machine.Snapshot.MachineId, key)))
            {
                _logger.LogWarning(
                    message:
                    "The scenario value of the key {Key} does not fit the value kind of the machine {MachineId}",
                    key,
                    machine.Snapshot.MachineId);
            }
        }
    }
}
