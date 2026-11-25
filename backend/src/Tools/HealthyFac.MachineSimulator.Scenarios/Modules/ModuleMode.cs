using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.MachineSimulator.Scenarios.Modules;

/// <summary>
/// The mode of a module: normal values, a drain toward the failure, or the failure itself.
/// </summary>
public sealed class ModuleMode : Enumeration<ModuleMode>
{
    public static readonly ModuleMode Expected = new(value: "expected");
    public static readonly ModuleMode Draining = new(value: "draining");
    public static readonly ModuleMode Failure = new(value: "failure");

    private ModuleMode(string value)
        : base(value)
    {
    }
}
