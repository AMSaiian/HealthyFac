namespace HealthyFac.Shared.UnitTests.Enumerations.Data;

public static class EnumerationTestData
{
    public const string UnknownValue = "notADeclaredValue";

    public static TheoryData<string> UnknownValues =>
    [
        UnknownValue,
        "operation",
    ];

    public static TheoryData<string> MiscasedKnownValues =>
    [
        "OPERATIONAL",
    ];

    public static TheoryData<string, MachineStateTestEnumeration> KnownValues => new()
    {
        { "operational", MachineStateTestEnumeration.Operational },
        { "inMaintenance", MachineStateTestEnumeration.InMaintenance },
    };

    public static TheoryData<IReadOnlyList<string>> DeclarationOrder =>
    [
        new[] { "operational", "inMaintenance", "outOfService" },
    ];
}
