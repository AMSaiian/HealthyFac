using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

public sealed class ServiceTokenOptions
{
    public const string SectionName = "ServiceTokens";

    public required string TokenEndpoint { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    /// <summary>
    /// The time in seconds before the token expiry at which the cache fetches a new token, so a request never leaves
    /// with a token that expires on the way. A margin at or above the token lifetime makes every call fetch a token.
    /// </summary>
    public int RefreshMarginSeconds { get; init; } = 30;

    /// <summary>
    /// The longest random time in seconds that the cache adds to <see cref="RefreshMarginSeconds"/> for each token.
    /// The instances of a service then do not fetch new tokens at the same moment. A token is replaced between the
    /// margin and the margin plus this value before its expiry.
    /// </summary>
    public int RefreshJitterSeconds { get; init; } = 30;

    /// <summary>
    /// The longest time in seconds that one instance holds the fetch lock of a token, and the longest time that a
    /// waiting instance waits for the token of the holder. The fetch from the token endpoint gets the same limit. A
    /// waiter that reaches the limit fetches the token itself, so a holder that crashed never blocks a call.
    /// </summary>
    public int LockTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// The time in milliseconds between two reads of a waiting instance while another instance fetches the token.
    /// It must be below <see cref="LockTimeoutSeconds"/>.
    /// </summary>
    public int LockPollMilliseconds { get; init; } = 100;
}

public sealed class ServiceTokenOptionsValidator : AbstractValidator<ServiceTokenOptions>
{
    public ServiceTokenOptionsValidator()
    {
        RuleFor(options => options.TokenEndpoint)
            .MustBeAbsoluteHttpUri()
            .WithMessage(ServiceTokenValidationMessages.TokenEndpointInvalid);

        RuleFor(options => options.ClientId)
            .NotEmpty()
            .WithMessage(ServiceTokenValidationMessages.ClientIdRequired);

        RuleFor(options => options.ClientSecret)
            .NotEmpty()
            .WithMessage(ServiceTokenValidationMessages.ClientSecretRequired);

        RuleFor(options => options.RefreshMarginSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.RefreshMarginInvalid);

        RuleFor(options => options.RefreshJitterSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.RefreshJitterInvalid);

        RuleFor(options => options.LockTimeoutSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.LockTimeoutInvalid);

        RuleFor(options => options.LockPollMilliseconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.LockPollInvalid);

        RuleFor(options => options)
            .Must(options => TimeSpan.FromMilliseconds(options.LockPollMilliseconds)
                < TimeSpan.FromSeconds(options.LockTimeoutSeconds))
            .When(options => options.LockTimeoutSeconds > 0 && options.LockPollMilliseconds > 0)
            .WithName(nameof(ServiceTokenOptions.LockPollMilliseconds))
            .WithMessage(ServiceTokenValidationMessages.LockPollNotBelowTimeout);
    }
}
