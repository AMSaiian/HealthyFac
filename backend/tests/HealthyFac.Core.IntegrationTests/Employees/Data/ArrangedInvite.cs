namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public sealed record ArrangedInvite(
    Guid CompanyId,
    IReadOnlyList<Guid> DepartmentIds,
    Guid InviteId,
    string Token
);
