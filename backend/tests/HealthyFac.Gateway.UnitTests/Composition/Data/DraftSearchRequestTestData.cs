namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class DraftSearchRequestTestData
{
    /// <summary>The case name, a body, and the field that the Gateway rejects.</summary>
    public static TheoryData<string, string, string> InvalidBodies => new()
    {
        { "no limit", """{"sort":{"column":"state","direction":"asc"}}""", "limit" },
        { "limit zero", """{"limit":0}""", "limit" },
        { "limit as text", """{"limit":"5"}""", "limit" },
        { "unknown sort column", """{"limit":5,"sort":{"column":"label","direction":"asc"}}""", "sort" },
        { "unknown sort direction", """{"limit":5,"sort":{"column":"state","direction":"up"}}""", "sort" },
        { "sort without direction", """{"limit":5,"sort":{"column":"state"}}""", "sort" },
        { "after of the wrong shape", """{"limit":5,"after":{"core":"x"}}""", "after" },
    };
}
