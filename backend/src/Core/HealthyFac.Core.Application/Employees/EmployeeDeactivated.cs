using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Core.Application.Employees;

public sealed record EmployeeDeactivated : IIntegrationMessage
{
    public static string MessageType => "core.v1:EmployeeDeactivated";

    public required Guid EmployeeId { get; init; }

    public required Guid CompanyId { get; init; }

    public Guid? IdentityUserId { get; init; }

    public required DateTime DeactivatedAt { get; init; }
}
