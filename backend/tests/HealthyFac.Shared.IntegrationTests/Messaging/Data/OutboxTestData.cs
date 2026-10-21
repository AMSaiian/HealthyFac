namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class OutboxTestData
{
    public const string BusinessChangeMessageType = "test.v1:BusinessChange";
    public const string ConcurrentMessageType = "test.v1:ConcurrentPublish";
    public const string BusinessChangeText = "rejected business change";
    public const int FirstAttemptCount = 1;
    public const int SecondAttemptCount = 2;
}
