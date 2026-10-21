using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class MessagingTestData
{
    public const string ConnectionString = "Host=localhost;Database=messaging_tests";
    public const string PingMessageType = "test.v1:Ping";
    public const string PingText = "ping";

    public const string PingPayload = """{"text":"ping"}""";

    public static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime EarlierProcessedOn = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
    public static readonly Guid InboxMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000010");

    public static InboxMessage CreateInboxMessage() =>
        new(InboxMessageId, PingMessageType, PingPayload, Now, traceParent: null);

    public static TheoryData<Type> ContextInterfaces =>
    [
        typeof(ITestServiceDbContext),
        typeof(IOutboxDbContext),
        typeof(IInboxDbContext),
    ];
}
