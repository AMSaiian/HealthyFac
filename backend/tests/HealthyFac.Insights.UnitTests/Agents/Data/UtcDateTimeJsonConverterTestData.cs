namespace HealthyFac.Insights.UnitTests.Agents.Data;

public static class UtcDateTimeJsonConverterTestData
{
    /// <summary>The case name and a time text that means 2026-11-26 08:00 UTC.</summary>
    public static TheoryData<string, string> UtcInstants => new()
    {
        { "Offset", "2026-11-26T10:00:00+02:00" },
        { "Zulu", "2026-11-26T08:00:00Z" },
        { "NoOffset", "2026-11-26T08:00:00" },
    };
}
