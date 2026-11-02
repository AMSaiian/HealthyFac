using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class DatabaseOutageTestData
{
    public const string InjectedFailureText = "simulated database timeout";
    public const string InboxInsertCommand = "INSERT INTO";
    public const string InboxTableName = "inbox_messages";
    public const int FailedCommandCount = MessagingConstants.MaxDeliveryAttempts;
    public const int RetryMaxCount = 5;

    public static readonly TimeSpan RetryMaxDelay = TimeSpan.FromMilliseconds(200);

    public static readonly Guid OutageMessageId = Guid.Parse("019a0000-0000-7000-8000-000000000105");
}
