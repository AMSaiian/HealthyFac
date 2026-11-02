namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class BrokerTestData
{
    public const ushort CloseReplyCode = 200;
    public const string CloseReplyText = "closed by the test";

    public static readonly TimeSpan GatewayProbeTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);
}
