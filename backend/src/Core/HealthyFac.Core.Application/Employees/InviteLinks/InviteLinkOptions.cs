using FluentValidation;
using HealthyFac.Shared.Application.Configuration;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.Application.Employees.InviteLinks;

public sealed class InviteLinkOptions
{
    public const string SectionName = "InviteLinks";

    public required string SpaBaseUrl { get; init; }
}

public sealed class InviteLinkOptionsValidator : AbstractValidator<InviteLinkOptions>
{
    public InviteLinkOptionsValidator()
    {
        RuleFor(options => options.SpaBaseUrl)
            .MustBeAbsoluteHttpUri()
            .WithMessage(InviteLinkValidationMessages.SpaBaseUrlInvalid)
            .SingleLineText();
    }
}
