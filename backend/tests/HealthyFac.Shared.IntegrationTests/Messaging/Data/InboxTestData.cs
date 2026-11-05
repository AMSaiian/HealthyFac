namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class InboxTestData
{
    public const string MessageType = "test.v1:Ping";
    public const string Payload = """{"text":"ping","count":2}""";
    public const string TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

    public static readonly Guid ActorId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid CompanyId = Guid.Parse(input: "019a0000-0000-7000-8000-0000000000c1");

    public static readonly DateTime Now = new(
        year: 2026, month: 10, day: 21, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly Guid NewMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000001");
    public static readonly Guid DuplicateMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000002");
    public static readonly Guid RolledBackMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000003");
    public static readonly Guid ProcessedMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000004");
    public static readonly Guid ConcurrentMessageId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000005");
}
