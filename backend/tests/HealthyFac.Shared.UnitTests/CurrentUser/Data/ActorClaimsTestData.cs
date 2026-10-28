using System.Security.Claims;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ActorClaimsTestData
{
    public static ClaimsPrincipal CreatePrincipal(
        string? subject,
        IEnumerable<string> organizations,
        string? employeeId
    )
    {
        var claims = organizations
            .Select(organization => new Claim(KeycloakClaimTypes.Organization, organization))
            .ToList();

        if (subject is not null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        }

        if (employeeId is not null)
        {
            claims.Add(new Claim(KeycloakClaimTypes.EmployeeId, employeeId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    public static ClaimsPrincipal ValidPrincipal => CreatePrincipal(
        ActorTestData.UserId.ToString(),
        [ActorTestData.CompanyId.ToString()],
        ActorTestData.EmployeeId.ToString());

    public static TheoryData<string> JsonOrganizationValues => new()
    {
        $"[\"{ActorTestData.CompanyId}\"]",
        "[]",
        $"[\"{ActorTestData.CompanyId}\",\"{ActorTestData.OtherCompanyId}\"]",
        "[\"not-a-guid\"]",
        "[1]",
        "{\"id\":1}",
        "[\"",
    };
}
