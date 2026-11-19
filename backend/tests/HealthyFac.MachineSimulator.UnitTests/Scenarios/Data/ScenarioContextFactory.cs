using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

/// <summary>
/// Builds scenario contexts for the scenario tests: a fixed random seed, the run time, and module states at a drain
/// progress.
/// </summary>
public static class ScenarioContextFactory
{
    public const int Seed = 20261116;

    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);

    public static ScenarioContext Create(
        IMachineScenario scenario,
        double runSeconds,
        IReadOnlyDictionary<ModuleName, ModuleState>? modules = null,
        bool isStopped = false,
        double elapsedSeconds = 1
    ) =>
        new()
        {
            RunTime = TimeSpan.FromSeconds(runSeconds),
            Elapsed = TimeSpan.FromSeconds(elapsedSeconds),
            Modules = modules ?? CreateModules(scenario),
            Counters = scenario.CounterStartValues,
            IsStopped = isStopped,
            Random = new Random(Seed),
            Memory = new Dictionary<string, double>(),
        };

    public static Dictionary<ModuleName, ModuleState> CreateModules(IMachineScenario scenario) =>
        scenario.Modules.ToDictionary(module => module.Name, module => new ModuleState(module.Kind));

    public static Dictionary<ModuleName, ModuleState> ModulesWith(
        IMachineScenario scenario,
        ModuleName name,
        ModuleMode mode,
        double progress = 0
    )
    {
        var modules = CreateModules(scenario);
        modules[name].SetMode(mode);
        if (mode == ModuleMode.Draining)
        {
            modules[name].Advance(DrainDuration * progress, DrainDuration);
        }

        return modules;
    }
}
