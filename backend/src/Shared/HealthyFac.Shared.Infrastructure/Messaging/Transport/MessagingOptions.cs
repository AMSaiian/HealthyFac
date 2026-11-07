using FluentValidation;

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

public sealed class MessagingOptionsValidator : AbstractValidator<MessagingOptions>
{
    public MessagingOptionsValidator()
    {
        RuleFor(options => options.QueueName)
            .NotEmpty()
            .WithMessage(MessagingValidationMessages.QueueNameRequired);

        RuleFor(options => options.PollingIntervalMilliseconds)
            .InclusiveBetween(
                MessagingConstants.Limits.MinPollingIntervalMilliseconds,
                MessagingConstants.Limits.MaxPollingIntervalMilliseconds)
            .WithMessage(MessagingValidationMessages.PollingIntervalOutOfRange);

        RuleFor(options => options.InboxMaxAttemptCount)
            .InclusiveBetween(
                MessagingConstants.Limits.MinInboxAttemptCount,
                MessagingConstants.Limits.MaxInboxAttemptCount)
            .WithMessage(MessagingValidationMessages.InboxAttemptCountOutOfRange);
    }
}
