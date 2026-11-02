using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Auth.Application.Identities;

public sealed record IdentityProvisioningFailed : IIntegrationMessage
{
    public static string MessageType => "auth.v1:IdentityProvisioningFailed";

    public required Guid EmployeeId { get; init; }

    public required string Reason { get; init; }
}
