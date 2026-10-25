using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Core.Application.Employees.Messages;

public sealed record EmployeeAccepted : IIntegrationMessage
{
    public static string MessageType => "core.v1:EmployeeAccepted";

    public required Guid EmployeeId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string RoleCode { get; init; }
}
