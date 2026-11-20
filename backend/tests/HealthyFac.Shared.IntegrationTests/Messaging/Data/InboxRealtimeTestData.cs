namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class InboxRealtimeTestData
{
    public const string MessageType = "test.v1:RealtimePush";
    public const string FailsOnceMarker = "fails once";
    public const string HandledText = "realtime handled";
    public const string FailsOnceText = "realtime fails once";
    public const string RepeatedText = "realtime repeated";
    public const string UpdateCommand = "UPDATE";

    public static readonly Guid HandledRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000301");
    public static readonly Guid FailsOnceRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000302");
    public static readonly Guid RepeatedRowId = Guid.Parse(input: "019a0000-0000-7000-8000-000000000303");
}
