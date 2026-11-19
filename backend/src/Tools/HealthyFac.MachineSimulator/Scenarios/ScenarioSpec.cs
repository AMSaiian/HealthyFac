using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The data of a detailed scenario. <see cref="DetailedScenario"/> turns it into samples.
/// </summary>
public sealed record ScenarioSpec
{
    public required ScenarioKind Kind { get; init; }

    public required IReadOnlyList<ModuleDefinition> Modules { get; init; }

    public required IReadOnlyDictionary<string, double> CounterStartValues { get; init; }

    /// <summary>Makes the values of the running phase at the current run time, before the module effects.</summary>
    public required Func<ScenarioContext, PhaseSample> Running { get; init; }

    /// <summary>Makes the stopped values. A maintenance state and a trip failure both use them.</summary>
    public required Func<ScenarioContext, IReadOnlyDictionary<string, object>> Stopped { get; init; }

    /// <summary>The state metric and value that a trip failure sets, or <see langword="null"/>.</summary>
    public (string Key, string Value)? FaultState { get; init; }
}
