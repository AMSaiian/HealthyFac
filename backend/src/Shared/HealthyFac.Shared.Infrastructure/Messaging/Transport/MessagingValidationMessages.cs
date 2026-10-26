namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Defines the validator messages for the messaging configuration.
/// </summary>
public static class MessagingValidationMessages
{
    public const string QueueNameRequired = "The messaging queue name is required.";

    public static readonly string PollingIntervalOutOfRange =
        $"The messaging polling interval must be from {MessagingConstants.Limits.MinPollingIntervalMilliseconds} "
        + $"to {MessagingConstants.Limits.MaxPollingIntervalMilliseconds} milliseconds.";

    public static readonly string InboxAttemptCountOutOfRange =
        $"The inbox attempt limit must be from {MessagingConstants.Limits.MinInboxAttemptCount} "
        + $"to {MessagingConstants.Limits.MaxInboxAttemptCount} attempts.";
}
