using FluentValidation;

namespace HealthyFac.Auth.Infrastructure.Revocation;

public sealed class RevocationOptionsValidator : AbstractValidator<RevocationOptions>
{
    public RevocationOptionsValidator()
    {
        RuleFor(options => options.TokenLifetimeSeconds)
            .GreaterThan(0)
            .WithMessage(RevocationValidationMessages.TokenLifetimeInvalid);

        RuleFor(options => options.ClockSkewSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage(RevocationValidationMessages.ClockSkewInvalid);
    }
}
