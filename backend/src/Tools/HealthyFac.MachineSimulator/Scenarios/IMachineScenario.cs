using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The behavior of one kind of machine: its modules, its counters, and its samples.
/// </summary>
public interface IMachineScenario
{
    ScenarioKind Kind { get; }

    /// <summary>The modules of the machine, each with its effect on the machine (trip or warning).</summary>
    IReadOnlyList<ModuleDefinition> Modules { get; }

    /// <summary>The start value of each counter by metric key.</summary>
    IReadOnlyDictionary<string, double> CounterStartValues { get; }

    /// <summary>
    /// Makes the sample of the current moment.
    /// </summary>
    /// <param name="context">The run time, the modules, the counters, and the random source.</param>
    /// <returns>The values and the codes to send.</returns>
    ScenarioSample Sample(ScenarioContext context);
}
