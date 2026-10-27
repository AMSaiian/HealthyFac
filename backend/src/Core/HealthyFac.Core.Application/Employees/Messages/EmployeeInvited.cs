using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Employees.Messages;

public sealed record EmployeeInvited : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:EmployeeInvited";

    public required Guid InviteId { get; init; }

    public required Guid CompanyId { get; init; }

    [SensitiveData]
    public required string Email { get; init; }
}
