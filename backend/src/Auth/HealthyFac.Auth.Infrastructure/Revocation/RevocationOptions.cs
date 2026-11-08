using FluentValidation;

namespace HealthyFac.Auth.Infrastructure.Revocation;

/// <summary>
/// Sets how long a revocation entry lives. The entry must outlive every access token that the user holds, so the
/// time to live is the token lifetime plus the clock skew that the gateway tolerates.
/// </summary>
public sealed class RevocationOptions
{
    public const string SectionName = "Revocation";

    public int TokenLifetimeSeconds { get; init; } = 300;

    public int ClockSkewSeconds { get; init; } = 60;
}

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
