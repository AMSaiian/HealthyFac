using System.Security.Claims;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.UnitTests.Realtime.Data;

public static class RealtimeHubTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0f1b4c0e-3c3a-4d57-9a53-3c9b8a1d2e10");

    public static readonly IReadOnlyList<string> SupervisorPermissions =
    [
        "work-orders:manage",
        "drafts:read",
        "employees:manage",
        "cases:read",
        "work-orders:read",
        "shifts:read",
        "machines:read",
        "definition-drafts:read",
    ];

    public static readonly IReadOnlyList<string> WorkerPermissions =
    [
        "drafts:read",
        "cases:read",
        "work-orders:read",
        "shifts:read",
        "machines:read",
        "definition-drafts:read",
    ];

    public static readonly IReadOnlyList<string> WorkerExtraPermissions = [.. WorkerPermissions, "machines:delete"];

    public static readonly IReadOnlyList<string> WorkerGroups =
    [
        RealtimeGroups.ForKind(CompanyId, RealtimeNotificationKind.WorkOrderDraftCreated),
        RealtimeGroups.ForKind(CompanyId, RealtimeNotificationKind.MaintenanceCaseOpened),
        RealtimeGroups.ForTopic(CompanyId, RealtimeTopic.WorkOrders),
        RealtimeGroups.ForTopic(CompanyId, RealtimeTopic.Schedule),
        RealtimeGroups.ForTopic(CompanyId, RealtimeTopic.Machines),
        RealtimeGroups.ForTopic(CompanyId, RealtimeTopic.Cases),
        RealtimeGroups.ForTopic(CompanyId, RealtimeTopic.DefinitionDrafts),
    ];

    public static readonly IReadOnlyList<string> SupervisorGroups =
    [
        .. WorkerGroups,
        RealtimeGroups.ForKind(CompanyId, RealtimeNotificationKind.WorkOrderBlocked),
        RealtimeGroups.ForKind(CompanyId, RealtimeNotificationKind.ProvisioningFailed),
    ];

    public static readonly Guid UserId = Guid.Parse(input: "7d1c9a52-5a0b-4f5e-8d3a-2b6e4c1f9a01");

    public static readonly Guid EmployeeId = Guid.Parse(input: "3e8b2f60-9c4d-4a1e-b7d5-6a0c8e2f1b02");

    public static ClaimsPrincipal CreateUser(
        IReadOnlyList<string> permissions,
        string? organization,
        bool hasEmployee = true
    )
    {
        var claims = permissions.Select(permission => new Claim(KeycloakClaimTypes.Permissions, permission)).ToList();
        if (organization is not null)
        {
            claims.Add(new Claim(KeycloakClaimTypes.Organization, organization));
        }

        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()));
        if (hasEmployee)
        {
            claims.Add(new Claim(KeycloakClaimTypes.EmployeeId, EmployeeId.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    public static TheoryData<string?, bool> IncompleteUsers => new()
    {
        { null, true },
        { CompanyText, false },
    };

    public static string CompanyText => CompanyId.ToString();
}
