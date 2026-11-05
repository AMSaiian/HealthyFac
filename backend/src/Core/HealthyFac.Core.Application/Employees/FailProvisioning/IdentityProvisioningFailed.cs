using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Employees.FailProvisioning;

public sealed record IdentityProvisioningFailed : IIntegrationMessage, IRequest
{
    public static string MessageType => "auth.v1:IdentityProvisioningFailed";

    public required Guid EmployeeId { get; init; }

    public required string Reason { get; init; }
}

public class IdentityProvisioningFailedValidator : AbstractValidator<IdentityProvisioningFailed>
{
    public IdentityProvisioningFailedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.Reason)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.ReasonRequired)
            .SingleLineText();
    }
}
