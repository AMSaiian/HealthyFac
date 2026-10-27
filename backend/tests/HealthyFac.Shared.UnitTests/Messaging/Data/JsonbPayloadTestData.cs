namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class JsonbPayloadTestData
{
    public static TheoryData<string> StorablePayloads => new()
    {
        """{"a":1}""",
        "[1,2,3]",
        "null",
        """{"text":"line\nbreak"}""",
    };

    public static TheoryData<string> NotStorablePayloads => new()
    {
        "{",
        string.Empty,
        """{"a":1}x""",
        """{"\u0000":1}""",
        """{"a":"\u0000"}""",
        """{"a":"\ud800"}""",
    };
}
