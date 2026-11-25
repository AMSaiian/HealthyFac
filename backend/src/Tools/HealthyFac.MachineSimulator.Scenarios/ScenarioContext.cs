using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// What a scenario reads to make one sample of one machine.
/// </summary>
public sealed class ScenarioContext
{
    /// <summary>The run time since the start. It gives the position in the cycle and stops with the machine.</summary>
    public required TimeSpan RunTime { get; init; }

    /// <summary>The time since the last sample. It drives the slow cooling.</summary>
    public required TimeSpan Elapsed { get; init; }

    public required IReadOnlyDictionary<ModuleName, ModuleState> Modules { get; init; }

    /// <summary>The counter values by key.</summary>
    public required IReadOnlyDictionary<string, double> Counters { get; init; }

    /// <summary><see langword="true"/> in maintenance or out of service: the machine shows stopped values.</summary>
    public required bool IsStopped { get; init; }

    public required Random Random { get; init; }

    /// <summary>The definitions of the machine. Only the generic scenario reads them.</summary>
    public MachineDefinitions Definitions { get; init; } = MachineDefinitions.Empty;

    /// <summary>Values that live between samples of one machine (the temperature that a stop cools from).</summary>
    public required IDictionary<string, double> Memory { get; init; }
}
