using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Core.Application.Employees.Messages;

public sealed record EmployeeInvited : IIntegrationMessage
{
    public static string MessageType => "core.v1:EmployeeInvited";

    public required Guid InviteId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Email { get; init; }
}
