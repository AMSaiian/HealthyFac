using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Emails;

namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

public sealed record EmployeeAccepted : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:EmployeeAccepted";

    public required Guid EmployeeId { get; init; }

    public required Guid CompanyId { get; init; }

    [SensitiveData]
    public required string Email
    {
        get;
        init => field = EmailAddresses.Normalize(value);
    }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }

    public required string RoleCode { get; init; }
}
