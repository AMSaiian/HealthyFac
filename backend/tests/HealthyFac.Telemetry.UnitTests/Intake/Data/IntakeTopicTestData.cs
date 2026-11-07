namespace HealthyFac.Telemetry.UnitTests.Intake.Data;

public static class IntakeTopicTestData
{
    public const string ValidGatewayId = "abcdefghijklmnop";

    public static TheoryData<string> InvalidTopics => new()
    {
        string.Empty,
        "healthyfac/gateways/abcdefghijklmnop",
        "healthyfac/gateways/abcdefghijklmnop/messages/extra",
        "healthyfac/gateways//messages",
        "healthyfac/gateways/ABCDEFGHIJKLMNOP/messages",
        "healthyfac/gateways/abcdefghijklmno/messages",
        "healthyfac/gateways/abcdefghijklmnopq/messages",
        "healthyfac/gateways/abcdefgh/jklmnop/messages",
        "other/abcdefghijklmnop/messages",
        "/healthyfac/gateways/abcdefghijklmnop/messages",
    };
}
