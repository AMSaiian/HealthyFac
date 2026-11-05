using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Employees.CompleteProvisioning;

public sealed record IdentityProvisioned : IIntegrationMessage, IRequest
{
    public static string MessageType => "auth.v1:IdentityProvisioned";

    public required Guid EmployeeId { get; init; }

    public required Guid IdentityUserId { get; init; }
}

public class IdentityProvisionedValidator : AbstractValidator<IdentityProvisioned>
{
    public IdentityProvisionedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.IdentityUserId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.IdentityUserIdRequired);
    }
}
