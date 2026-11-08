using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class TransportTestData
{
    public const string QueueName = "transport-tests";
    public const string ErrorQueueName = "error";
    public const string PingMessageType = "test.v1:Ping";
    public const string PingPayload = """{"text":"ping"}""";
    public const string BlankTextPayload = """{"text":""}""";
    public const string NotJsonPayload = "{";
    public const string NullCharacterEscapePayload = """{"name":"\u0000"}""";
    public const string BrokerConnectionStringKey = "ConnectionStrings:RabbitMq";
    public const string QueueNameKey = "Messaging:QueueName";
    public const string PollingIntervalKey = "Messaging:PollingIntervalMilliseconds";
    public const string ServiceNameKey = "Service:Name";
    public const string PollingIntervalMilliseconds = "50";
    public const string MessageIdHeader = "rbs2-msg-id";
    public const string ErrorDetailsHeader = "rbs2-error-details";
    public const string FailedOnceText = "1 unhandled exceptions";

    public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(seconds: 30);
    public static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(milliseconds: 100);
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(seconds: 1);

    public static readonly Guid ActorId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c1");
    public static readonly Guid DeliveredMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000101");
    public static readonly Guid DuplicatedMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000102");
    public static readonly Guid SentinelMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000103");
    public static readonly Guid LoggedMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000106");
    public static readonly Guid HeaderlessMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000104");
    public static readonly Guid InvalidPayloadMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000107");
    public static readonly Guid InvalidSentinelMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-00000000010a");
    public static readonly Guid NotJsonMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-00000000010b");
    public static readonly Guid NullCharacterMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-00000000010c");

    public static TheoryData<string, Guid, bool> FailFastDeliveries => new()
    {
        { PingPayload, HeaderlessMessageId, false },
        { NotJsonPayload, NotJsonMessageId, true },
        { NullCharacterEscapePayload, NullCharacterMessageId, true },
    };

    public static Dictionary<string, string> ActorHeaders() =>
        new()
        {
            [ActorHeaderNames.ActorId] = ActorId.ToString(),
            [ActorHeaderNames.CompanyId] = CompanyId.ToString(),
        };
}
