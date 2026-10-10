namespace HealthyFac.Shared.UnitTests.Enumerations.Data;

public static class EnumerationTestData
{
    public const string UnknownValue = "notADeclaredValue";

    public static TheoryData<string> UnknownValues => new()
    {
        UnknownValue,
        "operation",
    };

    public static TheoryData<string> MiscasedKnownValues => new()
    {
        "Operational",
        "OPERATIONAL",
        "INMAINTENANCE",
        "inmaintenance",
    };

    public static TheoryData<string, MachineStateTestEnumeration> KnownValues => new()
    {
        { "operational", MachineStateTestEnumeration.Operational },
        { "inMaintenance", MachineStateTestEnumeration.InMaintenance },
        { "outOfService", MachineStateTestEnumeration.OutOfService },
    };

    public static TheoryData<IReadOnlyList<string>> DeclarationOrder => new()
    {
        new[] { "operational", "inMaintenance", "outOfService" },
    };
}
