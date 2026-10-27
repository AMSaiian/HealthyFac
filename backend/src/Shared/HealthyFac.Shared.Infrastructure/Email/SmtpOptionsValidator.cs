using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Email;

public sealed class SmtpOptionsValidator : AbstractValidator<SmtpOptions>
{
    public SmtpOptionsValidator()
    {
        RuleFor(options => options.Host)
            .NotEmpty()
            .WithMessage(EmailValidationMessages.HostRequired);

        RuleFor(options => options.Port)
            .InclusiveBetween(IPEndPoint.MinPort, IPEndPoint.MaxPort)
            .WithMessage(EmailValidationMessages.PortOutOfRange);

        RuleFor(options => options.TimeoutSeconds)
            .InclusiveBetween(EmailConstants.Timeout.MinSeconds, EmailConstants.Timeout.MaxSeconds)
            .WithMessage(EmailValidationMessages.TimeoutOutOfRange);

        RuleFor(options => options.FromAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(EmailValidationMessages.FromAddressInvalid)
            .EmailAddress()
            .WithMessage(EmailValidationMessages.FromAddressInvalid);

        RuleFor(options => options)
            .Must(options => string.IsNullOrWhiteSpace(options.UserName)
                == string.IsNullOrWhiteSpace(options.Password))
            .WithMessage(EmailValidationMessages.CredentialsIncomplete);
    }
}
