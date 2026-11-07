using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Email;

public sealed record SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }

    public int Port { get; init; } = EmailConstants.DefaultPort;

    public int TimeoutSeconds { get; init; } = EmailConstants.Timeout.DefaultSeconds;

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public required string FromAddress { get; init; }

    public required string FromName { get; init; }
}

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
