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
        { CorePermissions.WorkOrders.WriteCompany, [CorePermissions.WorkOrders.WriteCompany], "company" },
        {
            CorePermissions.WorkOrders.WriteCompany,
            [
                CorePermissions.WorkOrders.Read, CorePermissions.WorkOrders.Write,
                CorePermissions.WorkOrders.WriteCompany,
            ],
            "company"
        },
        {
            CorePermissions.WorkOrders.WriteCompany,
            [CorePermissions.WorkOrders.Read, CorePermissions.WorkOrders.Write],
            "own"
        },
        { CorePermissions.WorkOrders.WriteCompany, [CorePermissions.Shifts.WriteCompany], "own" },
        { CorePermissions.Shifts.WriteCompany, [CorePermissions.Shifts.WriteCompany], "company" },
        {
            CorePermissions.Shifts.WriteCompany, [CorePermissions.Shifts.Read, CorePermissions.WorkOrders.WriteCompany],
            "own"
        },
        { CorePermissions.Shifts.WriteCompany, [], "own" },
    };
}
