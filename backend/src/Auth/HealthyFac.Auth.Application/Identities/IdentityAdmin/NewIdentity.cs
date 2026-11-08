using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Auth.Application.Identities.IdentityAdmin;

public sealed record NewIdentity
{
    public required Guid EmployeeId { get; init; }

    [SensitiveData]
    public required string Email { get; init; }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }
}
