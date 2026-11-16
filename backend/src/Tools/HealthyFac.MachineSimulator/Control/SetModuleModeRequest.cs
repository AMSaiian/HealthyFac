using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.MachineSimulator.Control;

/// <summary>
/// Sets the mode of one module of a machine.
/// </summary>
public sealed record SetModuleModeRequest
{
    public ModuleMode? Mode { get; init; }
}
