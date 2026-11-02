using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CurrentUser;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ActorClaimsTestData
{
    public const string OtherServiceClientId = "healthyfac-gateway";

    public static ClaimsPrincipal CreatePrincipal(
        string? subject,
        IEnumerable<string> organizations,
        string? employeeId,
        string? clientId = null
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

        if (clientId is not null)
        {
            claims.Add(new Claim(KeycloakClaimTypes.ClientId, clientId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    public static ClaimsPrincipal ValidPrincipal => CreatePrincipal(
        ActorTestData.UserId.ToString(),
        [ActorTestData.CompanyId.ToString()],
        ActorTestData.EmployeeId.ToString());

    public static ActorContext CallerActor => ServiceActor.InboxProcessor.For(ServiceName.Auth);

    public static ServiceActorOptions NoServiceClients => new();

    public static ServiceActorOptions AllowedServiceClients => new()
    {
        AllowedClients = [IncomingTokenTestData.ServiceClientId],
    };

    public static HeaderDictionary NoHeaders => new();

    public static HeaderDictionary ActorHeaders(string? actorId, string? companyId = null)
    {
        var headers = new HeaderDictionary();
        if (actorId is not null)
        {
            headers[ActorHeaderNames.ActorId] = actorId;
        }

        if (companyId is not null)
        {
            headers[ActorHeaderNames.CompanyId] = companyId;
        }

        return headers;
    }

    public static TheoryData<ServiceActorOptions> OptionsWithoutServiceClient => new()
    {
        new ServiceActorOptions(),
        new ServiceActorOptions { AllowedClients = [OtherServiceClientId] },
        new ServiceActorOptions { AllowedClients = [IncomingTokenTestData.ServiceClientId.ToUpperInvariant()] },
    };

    public static TheoryData<string> InvalidActorHeaders => new()
    {
        "not-a-guid",
        "",
        Guid.Empty.ToString(),
    };

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
