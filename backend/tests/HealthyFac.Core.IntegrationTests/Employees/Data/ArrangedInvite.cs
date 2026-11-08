namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public sealed record ArrangedInvite(
    Guid CompanyId,
    Guid InviteId,
    string Token
);
