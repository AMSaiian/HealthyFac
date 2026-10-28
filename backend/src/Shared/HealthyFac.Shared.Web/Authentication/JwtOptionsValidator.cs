using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.Authentication;

public sealed class JwtOptionsValidator : AbstractValidator<JwtOptions>
{
    public JwtOptionsValidator()
    {
        RuleFor(options => options.Issuer)
            .MustBeAbsoluteHttpUri()
            .WithMessage(AuthenticationValidationMessages.IssuerInvalid);

        RuleFor(options => options.MetadataAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(AuthenticationValidationMessages.MetadataAddressInvalid);

        RuleFor(options => options.Audience)
            .NotEmpty()
            .WithMessage(AuthenticationValidationMessages.AudienceMissing);
    }
}
