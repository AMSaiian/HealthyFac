using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Employees.Messages;

public sealed record IdentityProvisioned : IIntegrationMessage, IRequest
{
    public static string MessageType => "auth.v1:IdentityProvisioned";

    public required Guid EmployeeId { get; init; }

    public required Guid IdentityUserId { get; init; }
}
