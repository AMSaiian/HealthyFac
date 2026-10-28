using System.Security.Claims;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Authentication.Data;

public static class PermissionPoliciesTestData
{
    public const string InvitePermission = "employees:invite";
    public const string MachinesPermission = "machines:write";

    private const string AuthenticationType = "test";

    public static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPermissionPolicies(new HashSet<string> { InvitePermission, MachinesPermission });

        return services.BuildServiceProvider();
    }

    public static ClaimsPrincipal CreateUser(params string[] permissions) =>
        new(
            new ClaimsIdentity(
                permissions.Select(permission => new Claim(KeycloakClaimTypes.Permissions, permission)),
                AuthenticationType));
}
