using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Simulation;

namespace HealthyFac.MachineSimulator.Control;

public sealed record SimulatedMachineResponse
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

    public required IReadOnlyList<ModuleResponse> Modules { get; init; }

    public static SimulatedMachineResponse From(MachineView view) =>
        new()
        {
            MachineId = view.MachineId,
            CompanyId = view.CompanyId,
            Name = view.Name,
            SerialNumber = view.SerialNumber,
            GatewayId = view.GatewayId,
            Scenario = view.Scenario,
            CoreState = view.CoreState,
            ExpectedGoLiveOn = view.ExpectedGoLiveOn,
            IsRegistered = view.IsRegistered,
            IsLive = view.IsLive,
            IsRunning = view.IsRunning,
            IsPublishing = view.IsPublishing,
            Counters = view.Counters,
            Modules = view.Modules.Select(ModuleResponse.From).ToList(),
        };
}

public sealed record ModuleResponse
{
    public required ModuleName Module { get; init; }

    public required ModuleKind Kind { get; init; }

    public required ModuleMode Mode { get; init; }

    public required double DrainProgress { get; init; }

    public static ModuleResponse From(ModuleView view) =>
        new() { Module = view.Module, Kind = view.Kind, Mode = view.Mode, DrainProgress = view.DrainProgress };
}
