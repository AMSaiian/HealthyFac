using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class MessagingTestData
{
    public const string ConnectionString = "Host=localhost;Database=messaging_tests";
    public const string PingMessageType = "test.v1:Ping";
    public const string PingText = "ping";

    public static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    public static TheoryData<Type> ContextInterfaces =>
    [
        typeof(ITestServiceDbContext),
        typeof(IOutboxDbContext),
        typeof(IInboxDbContext),
    ];
}
