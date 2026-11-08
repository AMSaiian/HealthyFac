using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Emails;

namespace HealthyFac.Core.Messaging.Employees;

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
            .WithMessage(EmployeeMessageErrors.Validation.InviteIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(EmployeeMessageErrors.Validation.EmailInvalid)
            .MaximumLength(EmployeeMessageLimits.EmailMaxLength)
            .SingleLineText();
    }
}
