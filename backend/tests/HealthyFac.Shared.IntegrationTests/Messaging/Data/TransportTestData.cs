using HealthyFac.Shared.Infrastructure.Messaging.Transport;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class TransportTestData
{
    public const string QueueName = "transport-tests";
    public const string ErrorQueueName = "error";
    public const string PingMessageType = "test.v1:Ping";
    public const string PingPayload = """{"text":"ping"}""";
    public const string BrokerConnectionStringKey = "ConnectionStrings:RabbitMq";
    public const string QueueNameKey = "Messaging:QueueName";
    public const string PollingIntervalKey = "Messaging:PollingIntervalMilliseconds";
    public const string PollingIntervalMilliseconds = "50";
    public const string MessageIdHeader = "rbs2-msg-id";
    public const string ErrorDetailsHeader = "rbs2-error-details";
    public const string FailedOnceText = "1 unhandled exceptions";

    public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(1);

    public static readonly Guid ActorId = Guid.Parse("019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse("019a0000-0000-7000-8000-0000000000c1");
    public static readonly Guid DeliveredMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000101");
    public static readonly Guid DuplicatedMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000102");
    public static readonly Guid SentinelMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000103");
    public static readonly Guid LoggedMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000106");
    public static readonly Guid HeaderlessMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000104");

    public static Dictionary<string, string> ActorHeaders() =>
        new()
        {
            [MessageHeaders.ActorId] = ActorId.ToString(),
            [MessageHeaders.CompanyId] = CompanyId.ToString(),
        };
}
