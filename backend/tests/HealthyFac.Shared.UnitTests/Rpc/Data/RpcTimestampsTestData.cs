namespace HealthyFac.Shared.UnitTests.Rpc.Data;

public static class RpcTimestampsTestData
{
    /// <summary>The case name and a time whose kind is not UTC.</summary>
    public static TheoryData<string, DateTime> NonUtcTimes => new()
    {
        { "Local", new DateTime(year: 2026, month: 11, day: 26, hour: 8, minute: 0, second: 0, DateTimeKind.Local) },
        {
            "Unspecified",
            new DateTime(year: 2026, month: 11, day: 26, hour: 8, minute: 0, second: 0, DateTimeKind.Unspecified)
        },
    };
}
