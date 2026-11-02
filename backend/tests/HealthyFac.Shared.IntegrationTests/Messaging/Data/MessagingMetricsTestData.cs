namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class MessagingMetricsTestData
{
    public const string PublishedMessageType = "test.v1:MetricsPublished";
    public const string CancellingMessageType = "test.v1:Cancelling";
    public const string HandledText = "metrics handled";
    public const string RetriedText = "metrics retried";
    public const string AttemptLimitText = "metrics attempt limit";
    public const string CancelledText = "metrics cancelled";
    public const int SingleRowCount = 1;
    public const long OneMessage = 1;

    public const int PendingOutboxCount = 2;
    public const int PendingInboxCount = 1;
    public const double OutboxAgeSeconds = 30;
    public const double InboxAgeSeconds = 90;
    public const int NoMessages = 0;
    public const double NoAge = 0;

    public static readonly Guid HandledRowId = Guid.Parse("019a0000-0000-7000-8000-000000000501");
    public static readonly Guid RetriedRowId = Guid.Parse("019a0000-0000-7000-8000-000000000502");
    public static readonly Guid PermanentRowId = Guid.Parse("019a0000-0000-7000-8000-000000000503");
    public static readonly Guid AttemptLimitRowId = Guid.Parse("019a0000-0000-7000-8000-000000000504");
    public static readonly Guid CancelledRowId = Guid.Parse("019a0000-0000-7000-8000-000000000505");
    public static readonly Guid PendingInboxRowId = Guid.Parse("019a0000-0000-7000-8000-000000000506");
    public static readonly Guid FailedInboxRowId = Guid.Parse("019a0000-0000-7000-8000-000000000507");

    public static DateTime OutboxOccurredOn => InboxTestData.Now.AddSeconds(-OutboxAgeSeconds);

    public static DateTime InboxReceivedOn => InboxTestData.Now.AddSeconds(-InboxAgeSeconds);
}
