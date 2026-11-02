namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class OutboxTestData
{
    public const string BusinessChangeMessageType = "test.v1:BusinessChange";
    public const string ConcurrentMessageType = "test.v1:ConcurrentPublish";
    public const string ActorStampedMessageType = "test.v1:ActorStamped";
    public const string BusinessChangeText = "rejected business change";
    public const string ActorStampedText = "actor stamped";
    public const int FirstAttemptCount = 1;

    public static readonly Guid UserId = Guid.Parse("019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse("019a0000-0000-7000-8000-0000000000c1");
    public const int SecondAttemptCount = 2;
}
