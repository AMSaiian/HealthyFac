using HealthyFac.Shared.UnitTests.Enumerations.Data;

namespace HealthyFac.Shared.UnitTests.Serialization.Data;

public static class EnumerationJsonTestData
{
    public const string UnknownValueJson = """{"status":"notADeclaredValue"}""";

    public static TheoryData<MachineStateTestEnumeration, string> MembersWithJson => new()
    {
        { MachineStateTestEnumeration.Operational, """{"status":"operational","optional":null}""" },
        { MachineStateTestEnumeration.InMaintenance, """{"status":"inMaintenance","optional":null}""" },
    };
}
