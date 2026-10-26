namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class OutboxProcessorTestData
{
    public const string PublishedMessageType = "test.v1:ProcessorPublished";
    public const string ConcurrentMessageType = "test.v1:ProcessorConcurrent";
    public const string OutageMessageType = "test.v1:ProcessorOutage";
    public const string OutageBatchMessageType = "test.v1:ProcessorOutageBatch";
    public const string LockedMessageType = "test.v1:ProcessorLocked";
    public const string Payload = """{"text":"processor"}""";
    public const string SubscriberQueueName = "processor-subscriber";
    public const string DropOutboxTableSql = "DROP TABLE outbox_messages";
    public const string ForUpdateSkipLockedSql = "FOR UPDATE SKIP LOCKED";
    public const int ConcurrentRowCount = 20;
    public const int SingleRowCount = 1;
    public const int OutageBatchRowCount = 3;
    public const int FailedAttemptCount = 1;
}
