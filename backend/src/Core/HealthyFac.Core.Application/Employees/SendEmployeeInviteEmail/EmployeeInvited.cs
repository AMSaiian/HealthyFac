using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Emails;

namespace HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

public sealed record EmployeeInvited : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:EmployeeInvited";

    public required Guid InviteId { get; init; }

    public required Guid CompanyId { get; init; }

    [SensitiveData]
    public required string Email
    {
        get;
        init => field = EmailAddresses.Normalize(value);
    }
}

public class EmployeeInvitedValidator : AbstractValidator<EmployeeInvited>
{
    public EmployeeInvitedValidator()
    {
        RuleFor(message => message.InviteId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.InviteIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(EmployeeInviteErrors.Validation.EmailInvalid)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength)
            .SingleLineText();
    }
}
