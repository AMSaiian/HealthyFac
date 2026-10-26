namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Defines the settings that the outbox, the inbox, and their processors share.
/// </summary>
public static class MessagingConstants
{
    public const int MaxRetryDelaySeconds = 300;

    /// <summary>
    /// The number of delivery attempts that Rebus makes before it moves a message to the <c>error</c> queue. Rebus
    /// waits no time between attempts, but each attempt waits out a transient database failure inside the
    /// execution strategy of the service context, which takes about 30 seconds while the database refuses
    /// connections. 20 attempts then tolerate a database outage of about 10 minutes. The <c>error</c> queue is
    /// final, so a message may go there only after a retry cannot help.
    /// </summary>
    public const int MaxDeliveryAttempts = 20;

    public const string RabbitMqConnectionStringName = "RabbitMq";

    public static class Limits
    {
        public const int MinPollingIntervalMilliseconds = 10;
        public const int MaxPollingIntervalMilliseconds = 60000;
        public const int MinInboxAttemptCount = 1;
        public const int MaxInboxAttemptCount = 100;
    }

    public static class HealthCheck
    {
        public const string Name = "rabbitmq";

        public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
    }
}
