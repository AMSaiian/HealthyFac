namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

public static class IntakeTestData
{
    public const string SharedFilterFormat = "$share/{0}/healthyfac/gateways/+/messages";
    public const string PlainFilter = "healthyfac/gateways/+/messages";
    public const string GatewayTopicFormat = "healthyfac/gateways/{0}/messages";
    public const string OtherTopic = "other/topic";
    public const int MessageCount = 10;
    public const int SessionExpirySeconds = 60;
    public const int ShortSessionExpirySeconds = 3;
    public const int LargeMessageCount = 20;
    public const ushort ReceiveWindow = 2;
    public const string PayloadFormat = "{{\"index\":{0}}}";
    public static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(seconds: 15);
    public static readonly TimeSpan ExpiryWait = TimeSpan.FromSeconds(ShortSessionExpirySeconds + 3);
    public static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(seconds: 1);
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(seconds: 1);
}
