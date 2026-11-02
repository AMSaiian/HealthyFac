namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>The Rebus input queue of the service. Each service owns one queue.</summary>
    public required string QueueName { get; init; }

    public int PollingIntervalMilliseconds { get; init; } = 1000;

    /// <summary>
    /// The number of failed handling attempts after which an inbox row is failed and the processor stops claiming
    /// it. With the retry delays, 10 attempts take about 13 minutes.
    /// </summary>
    public int InboxMaxAttemptCount { get; init; } = 10;
}
