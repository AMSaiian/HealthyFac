using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Auth.Application.Identities.RevokeIdentity;

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
            .WithMessage(IdentityErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.IdentityUserId)
            .NotEqual(Guid.Empty)
            .When(message => message.IdentityUserId is not null)
            .WithMessage(IdentityErrors.Validation.IdentityUserIdInvalid);

        RuleFor(message => message.DeactivatedAt)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.DeactivatedAtRequired);
    }
}
