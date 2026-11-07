using FluentValidation;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Configures the limits per client address. The strict limit protects the anonymous invite endpoints.
/// The default limit applies to every other route that the gateway proxies.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimit Strict { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public FixedWindowLimit Default { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };
}

public sealed class RateLimitOptionsValidator : AbstractValidator<RateLimitOptions>
{
    public RateLimitOptionsValidator()
    {
        RuleFor(options => options.Strict.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Strict.PermitLimitTooSmall);

        RuleFor(options => options.Strict.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Strict.WindowTooSmall);

        RuleFor(options => options.Default.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Default.PermitLimitTooSmall);

        RuleFor(options => options.Default.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Default.WindowTooSmall);
    }
}
