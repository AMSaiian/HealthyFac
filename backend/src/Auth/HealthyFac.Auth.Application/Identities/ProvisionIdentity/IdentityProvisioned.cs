using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

public sealed record IdentityProvisioned : IIntegrationMessage
{
    public static string MessageType => "auth.v1:IdentityProvisioned";

    public required Guid EmployeeId { get; init; }

    public required Guid IdentityUserId { get; init; }
}
