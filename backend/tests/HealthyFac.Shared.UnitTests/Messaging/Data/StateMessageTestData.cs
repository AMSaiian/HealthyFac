using HealthyFac.Shared.UnitTests.Enumerations.Data;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class StateMessageTestData
{
    public const string StateJson = """{"state":"inMaintenance"}""";
    public const string UnknownStateJson = """{"state":"notADeclaredValue"}""";

    public static readonly StateMessage Message = new() { State = MachineStateTestEnumeration.InMaintenance };
}

/// <summary>A message record with an enumeration member.</summary>
public sealed record StateMessage
{
    public required MachineStateTestEnumeration State { get; init; }
}
