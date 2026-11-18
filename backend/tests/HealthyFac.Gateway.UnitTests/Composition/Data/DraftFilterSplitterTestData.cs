namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class DraftFilterSplitterTestData
{
    /// <summary>
    /// The case name, the filter of the request, then the expected filter of Core and of Telemetry. An expected value
    /// of <see langword="null"/> after a skip flag means "no filter to send".
    /// </summary>
    public static TheoryData<string, string?, bool, string?, bool, string?> Splits => new()
    {
        { "no filter", null, false, null, false, null },
        {
            "machine kind only",
            """{"kinds":{"values":["machine"]}}""",
            false, """{"kinds":{"values":["machine"]}}""",
            true, """{"kinds":{"values":[]}}"""
        },
        {
            "metric and status kinds only",
            """{"kinds":{"values":["metric","status"]}}""",
            true, """{"kinds":{"values":[]}}""",
            false, """{"kinds":{"values":["metric","status"]}}"""
        },
        {
            "machine and metric kinds",
            """{"kinds":{"values":["machine","metric"]}}""",
            false, """{"kinds":{"values":["machine"]}}""",
            false, """{"kinds":{"values":["metric"]}}"""
        },
        {
            "awaitingTarget state only",
            """{"states":{"values":["awaitingTarget"]}}""",
            true, """{"states":{"values":[]}}""",
            false, """{"states":{"values":["awaitingTarget"]}}"""
        },
        {
            "pending and awaitingTarget states",
            """{"states":{"values":["pending","awaitingTarget"]}}""",
            false, """{"states":{"values":["pending"]}}""",
            false, """{"states":{"values":["pending","awaitingTarget"]}}"""
        },
        {
            "kinds and states together",
            """{"kinds":{"values":["machine"]},"states":{"values":["applied"]}}""",
            false, """{"kinds":{"values":["machine"]},"states":{"values":["applied"]}}""",
            true, """{"kinds":{"values":[]},"states":{"values":["applied"]}}"""
        },
        {
            "a kind that no source knows goes to both",
            """{"kinds":{"values":["unknown"]}}""",
            false, """{"kinds":{"values":["unknown"]}}""",
            false, """{"kinds":{"values":["unknown"]}}"""
        },
        { "an empty filter object", "{}", false, "{}", false, "{}" },
        {
            "a value that is not a string goes to both",
            """{"kinds":{"values":[1,"machine"]}}""",
            false, """{"kinds":{"values":[1,"machine"]}}""",
            false, """{"kinds":{"values":[1]}}"""
        },
        {
            "a set filter that is not an object goes to both",
            """{"states":5}""",
            false, """{"states":5}""",
            false, """{"states":5}"""
        },
    };
}
