using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.UnitTests.Enumerations.Data;

/// <summary>
/// Enumeration used to exercise <see cref="Enumeration{TEnum}"/> discovery and lookup.
/// </summary>
public sealed class MachineStateTestEnumeration : Enumeration<MachineStateTestEnumeration>
{
    public static readonly MachineStateTestEnumeration Operational = new("operational");
    public static readonly MachineStateTestEnumeration InMaintenance = new("inMaintenance");
    public static readonly MachineStateTestEnumeration OutOfService = new("outOfService");

    private MachineStateTestEnumeration(string value)
        : base(value)
    {
    }
}
