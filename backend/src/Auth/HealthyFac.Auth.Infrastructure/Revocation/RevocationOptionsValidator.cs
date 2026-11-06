using FluentValidation;

namespace HealthyFac.Auth.Infrastructure.Revocation;

public sealed class RevocationOptionsValidator : AbstractValidator<RevocationOptions>
{
    public RevocationOptionsValidator()
    {
        RuleFor(options => options.TokenLifetimeSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(RevocationValidationMessages.TokenLifetimeInvalid);

        RuleFor(options => options.ClockSkewSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(RevocationValidationMessages.ClockSkewInvalid);
    }
}
