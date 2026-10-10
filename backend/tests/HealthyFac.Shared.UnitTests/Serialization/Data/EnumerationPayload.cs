using HealthyFac.Shared.UnitTests.Enumerations.Data;

namespace HealthyFac.Shared.UnitTests.Serialization.Data;

/// <summary>
/// Payload used to exercise enumeration serialization through a surrounding object.
/// </summary>
public sealed record EnumerationPayload
{
    public required MachineStateTestEnumeration Status { get; init; }

    public MachineStateTestEnumeration? Optional { get; init; }
}
