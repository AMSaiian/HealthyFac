using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Defines the operating states of a machine. The camelCase values reach the API and the database,
/// so never rename one in place.
/// </summary>
public sealed class MachineState : Enumeration<MachineState>
{
    public static readonly MachineState Operational = new("operational");
    public static readonly MachineState InMaintenance = new("inMaintenance");
    public static readonly MachineState OutOfService = new("outOfService");

    private MachineState(string value)
        : base(value)
    {
    }
}
