using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Emails;

namespace HealthyFac.Core.Messaging.Employees;

public sealed record EmployeeAccepted : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:EmployeeAccepted";

    public required Guid EmployeeId { get; init; }

    public required Guid CompanyId { get; init; }

    [SensitiveData]
    public required string Email
    {
        get;
        init => field = EmailAddresses.Normalize(value);
    }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }

    public required string RoleCode { get; init; }

    public required IReadOnlyList<Guid> DepartmentIds { get; init; }
}

public class EmployeeAcceptedValidator : AbstractValidator<EmployeeAccepted>
{
    public EmployeeAcceptedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.EmployeeIdRequired);

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

        RuleFor(message => message.FirstName)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.FirstNameRequired)
            .MaximumLength(EmployeeMessageLimits.NameMaxLength)
            .SingleLineText();

        RuleFor(message => message.LastName)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.LastNameRequired)
            .MaximumLength(EmployeeMessageLimits.NameMaxLength)
            .SingleLineText();

        RuleFor(message => message.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.DepartmentIdsRequired);

        RuleForEach(message => message.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.DepartmentIdEmpty);

        RuleFor(message => message.RoleCode)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.RoleCodeRequired)
            .SingleLineText();
    }
}
