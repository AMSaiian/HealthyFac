using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Employees.FailProvisioning;

public sealed record IdentityProvisioningFailed : IIntegrationMessage, IRequest
{
    public static string MessageType => "auth.v1:IdentityProvisioningFailed";

    public required Guid EmployeeId { get; init; }

    public required string Reason { get; init; }
}
