using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

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
