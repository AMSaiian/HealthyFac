using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class MessagingTestData
{
    public const string ConnectionString = "Host=localhost;Database=messaging_tests";
    public const string PingMessageType = "test.v1:Ping";
    public const string PingText = "ping";

    public const string PingPayload = """{"text":"ping"}""";
    public const string RetryError = "broker rejected the message";
    public const int ErrorMaxLength = 2000;
    public const int MaxRetryDelaySeconds = 300;
    public const int CappedRetryCount = 10;
    public const int HighAttemptLimit = 100;
    public const int SingleAttemptLimit = 1;
    public const int ThreeAttemptLimit = 3;

    public static readonly DateTime Now = new(
        year: 2026, month: 10, day: 21, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime EarlierProcessedOn = new(
        year: 2026, month: 10, day: 20, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly Guid ActorId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c1");
    public static readonly Guid InboxMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000010");

    public static InboxMessage CreateInboxMessage() =>
        new(InboxMessageId, PingMessageType, PingPayload, Now, traceParent: null, ActorId, CompanyId);

    public static OutboxMessage CreateOutboxMessage() =>
        new(PingMessageType, PingPayload, Now, traceParent: null, ActorId, CompanyId);

    public static List<TimeSpan> ScheduleRetries(OutboxMessage message, int count)
    {
        var delays = new List<TimeSpan>();
        for (var attempt = 0; attempt < count; attempt++)
        {
            message.ScheduleRetry(Now, RetryError);
            delays.Add(message.NextAttemptOn - Now);
        }

        return delays;
    }

    public static List<TimeSpan> RecordFailures(InboxMessage message, int count)
    {
        var delays = new List<TimeSpan>();
        for (var attempt = 0; attempt < count; attempt++)
        {
            message.RecordFailure(Now, RetryError, HighAttemptLimit);
            delays.Add(message.NextAttemptOn - Now);
        }

        return delays;
    }

    public static List<TimeSpan> ExpectedFirstDelays =>
        [TimeSpan.FromSeconds(seconds: 2), TimeSpan.FromSeconds(seconds: 4), TimeSpan.FromSeconds(seconds: 8)];

    public static TheoryData<Type> ContextInterfaces =>
    [
        typeof(ITestServiceDbContext),
        typeof(IOutboxDbContext),
        typeof(IInboxDbContext),
    ];
}
