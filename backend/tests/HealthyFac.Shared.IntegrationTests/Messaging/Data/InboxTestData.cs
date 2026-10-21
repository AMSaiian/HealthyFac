namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class InboxTestData
{
    public const string PostgresImage = "postgres:18.6-alpine";
    public const string MessageType = "test.v1:Ping";
    public const string Payload = """{"text":"ping","count":2}""";
    public const string TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

    public static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    public static readonly Guid NewMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000001");
    public static readonly Guid DuplicateMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000002");
    public static readonly Guid RolledBackMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000003");
    public static readonly Guid ProcessedMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000004");
}
