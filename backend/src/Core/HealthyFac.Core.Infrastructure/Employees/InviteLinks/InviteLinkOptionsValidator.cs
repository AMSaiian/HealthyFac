using FluentValidation;
using HealthyFac.Core.Application.Employees.InviteLinks;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Core.Infrastructure.Employees.InviteLinks;

public sealed class InviteLinkOptionsValidator : AbstractValidator<InviteLinkOptions>
{
    public InviteLinkOptionsValidator()
    {
        RuleFor(options => options.SpaBaseUrl)
            .MustBeAbsoluteHttpUri()
            .WithMessage(InviteLinkValidationMessages.SpaBaseUrlInvalid);
    }
}
