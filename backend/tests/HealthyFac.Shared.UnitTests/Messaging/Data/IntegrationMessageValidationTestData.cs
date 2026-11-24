namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class IntegrationMessageValidationTestData
{
    public const string BlankName = "";
    public const string MessageType = "test.v1:ValidatedMessage";

    public static readonly Type[] Types =
    [
        typeof(ValidatedMessage),
        typeof(ValidatedMessagePreHandlerCheck),
        typeof(ValidatedMessageHandler),
    ];
}
