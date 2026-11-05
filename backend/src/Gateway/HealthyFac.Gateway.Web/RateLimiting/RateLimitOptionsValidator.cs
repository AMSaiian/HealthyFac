using FluentValidation;

namespace HealthyFac.Gateway.Web.RateLimiting;

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
