using System.Security.Claims;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Core.UnitTests.Authorization.Data;

public static class AccessScopesTestData
{
    public static ClaimsPrincipal UserWith(params string[] permissions) =>
        new(
            new ClaimsIdentity(
                permissions.Select(permission => new Claim(KeycloakClaimTypes.Permissions, permission)),
                authenticationType: "test"));

    public static TheoryData<string, string[], string> Cases => new()
    {
        { CorePermissions.WorkOrders.Manage, [CorePermissions.WorkOrders.Manage], "company" },
        {
            CorePermissions.WorkOrders.Manage,
            [CorePermissions.WorkOrders.Read, CorePermissions.WorkOrders.Write, CorePermissions.WorkOrders.Manage],
            "company"
        },
        {
            CorePermissions.WorkOrders.Manage, [CorePermissions.WorkOrders.Read, CorePermissions.WorkOrders.Write],
            "own"
        },
        { CorePermissions.WorkOrders.Manage, [CorePermissions.Shifts.Manage], "own" },
        { CorePermissions.Shifts.Manage, [CorePermissions.Shifts.Manage], "company" },
        { CorePermissions.Shifts.Manage, [CorePermissions.Shifts.Read, CorePermissions.WorkOrders.Manage], "own" },
        { CorePermissions.Shifts.Manage, [], "own" },
    };
}
