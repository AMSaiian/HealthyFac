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
