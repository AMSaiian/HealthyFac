namespace HealthyFac.Telemetry.UnitTests.Intake.Data;

public static class MachineMessageParserTestData
{
    public const string GatewayId = "abcdefghijklmnop";
    public const string PayloadFormat = """{{"sampledAt":"{0}","metrics":{{}},"statuses":[]}}""";

    public static readonly DateTime ReceivedAt =
        new(year: 2026, month: 11, day: 6, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime SampledAt =
        new(year: 2026, month: 11, day: 6, hour: 10, minute: 14, second: 55, DateTimeKind.Utc);

    public static TheoryData<string> LenientSampledAtValues => new()
    {
        "2026-11-06T10:14:55Z",
        "2026-11-06t10:14:55Z",
        "2026-11-06T10:14:55z",
        "2026-11-06T12:14:55+02:00",
    };
}
