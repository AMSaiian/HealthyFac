using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Employees;

public sealed record EmployeeDeactivated : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:EmployeeDeactivated";

    public required Guid EmployeeId { get; init; }

    public required Guid CompanyId { get; init; }

    public Guid? IdentityUserId { get; init; }

    public required DateTime DeactivatedAt { get; init; }
}

public class EmployeeDeactivatedValidator : AbstractValidator<EmployeeDeactivated>
{
    public EmployeeDeactivatedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.IdentityUserId)
            .NotEqual(Guid.Empty)
            .When(message => message.IdentityUserId is not null)
            .WithMessage(EmployeeMessageErrors.Validation.IdentityUserIdInvalid);

        RuleFor(message => message.DeactivatedAt)
            .NotEmpty()
            .WithMessage(EmployeeMessageErrors.Validation.DeactivatedAtRequired);
    }
}
