using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// A copy of the state of one simulated machine for the control API. It is read inside the engine lock, so it never
/// shows a half-changed machine.
/// </summary>
public sealed record MachineView
{
    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public required string GatewayId { get; init; }

    public required ScenarioKind Scenario { get; init; }

    public required MachineState CoreState { get; init; }

    public required DateOnly ExpectedGoLiveOn { get; init; }

    public required bool IsRegistered { get; init; }

    public required bool IsLive { get; init; }

    public required bool IsRunning { get; init; }

    public required bool IsPublishing { get; init; }

    public required IReadOnlyDictionary<string, double> Counters { get; init; }

    public required IReadOnlyList<ModuleView> Modules { get; init; }
}

/// <summary>
/// The state of one module in a <see cref="MachineView"/>.
/// </summary>
public sealed record ModuleView
{
    public required ModuleName Module { get; init; }

    public required ModuleKind Kind { get; init; }

    public required ModuleMode Mode { get; init; }

    public required double DrainProgress { get; init; }
}
