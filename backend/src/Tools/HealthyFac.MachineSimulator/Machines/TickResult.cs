using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// The result of one tick of a publishing machine.
/// </summary>
public sealed record TickResult
{
    /// <summary>The sample to publish.</summary>
    public required ScenarioSample Sample { get; init; }

    /// <summary>The drain milestones that the tick crossed, with their module.</summary>
    public required IReadOnlyList<(ModuleName Module, DrainMilestone Milestone)> Milestones { get; init; }
}
