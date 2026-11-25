using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.MachineSimulator.Scenarios.Modules;

/// <summary>
/// The effect of a module failure. A trip failure stops the machine. A warning failure keeps it running.
/// </summary>
public sealed class ModuleKind : Enumeration<ModuleKind>
{
    public static readonly ModuleKind Trip = new(value: "trip");
    public static readonly ModuleKind Warning = new(value: "warning");

    private ModuleKind(string value)
        : base(value)
    {
    }
}
