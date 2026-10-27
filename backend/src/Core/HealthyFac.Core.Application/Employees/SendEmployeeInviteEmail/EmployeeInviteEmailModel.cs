using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

public sealed record EmployeeInviteEmailModel
{
    [SensitiveData]
    public required string To { get; init; }

    [SensitiveData]
    public required string Link { get; init; }

    public required DateTime ExpiresAt { get; init; }
}
