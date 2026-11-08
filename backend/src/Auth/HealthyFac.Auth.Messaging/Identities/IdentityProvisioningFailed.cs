using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Auth.Messaging.Identities;

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
            .WithMessage(IdentityMessageErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.Reason)
            .NotEmpty()
            .WithMessage(IdentityMessageErrors.Validation.ReasonRequired)
            .SingleLineText();
    }
}
