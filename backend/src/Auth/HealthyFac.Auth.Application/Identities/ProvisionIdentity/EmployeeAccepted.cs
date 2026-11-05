using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Emails;

namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

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
            .WithMessage(IdentityErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(IdentityErrors.Validation.EmailInvalid)
            .MaximumLength(IdentityValidationConstants.EmailMaxLength)
            .SingleLineText();

        RuleFor(message => message.FirstName)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.FirstNameRequired)
            .MaximumLength(IdentityValidationConstants.NameMaxLength)
            .SingleLineText();

        RuleFor(message => message.LastName)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.LastNameRequired)
            .MaximumLength(IdentityValidationConstants.NameMaxLength)
            .SingleLineText();

        RuleFor(message => message.DepartmentIds)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.DepartmentIdsRequired);

        RuleForEach(message => message.DepartmentIds)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.DepartmentIdEmpty);

        RuleFor(message => message.RoleCode)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.RoleCodeRequired)
            .SingleLineText();
    }
}
