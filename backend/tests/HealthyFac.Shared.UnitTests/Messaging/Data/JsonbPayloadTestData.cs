namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class JsonbPayloadTestData
{
    public static TheoryData<string> StorablePayloads =>
    [
        """{"a":1}""",
        """{"text":"line\nbreak"}""",
    ];

    public static TheoryData<string> NotStorablePayloads =>
    [
        "{",
        string.Empty,
        """{"a":1}x""",
        """{"\u0000":1}""",
        """{"a":"\u0000"}""",
        """{"a":"\ud800"}""",
    ];
}
