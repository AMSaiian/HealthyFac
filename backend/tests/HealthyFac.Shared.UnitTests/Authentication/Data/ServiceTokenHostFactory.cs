using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CurrentUser.ServiceActors;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.Authentication.Data;

/// <summary>
/// Starts an in-memory host with the JWT bearer handler, the permission policies, and the actor middleware, like a
/// service that accepts service tokens. The handler trusts the key of the <see cref="TestTokenFactory"/>. The
/// endpoint answers with the user ID that <see cref="ICurrentUserProvider"/> reports.
/// </summary>
public static class ServiceTokenHostFactory
{
    public const string InviteRoute = "/invite";
    public const string InvitePermission = "employees:invite";
    public const string AllowedClient = TestServiceTokenClaims.DefaultClientId;

    public static Task<IHost> StartAsync(TestTokenFactory tokens) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(Settings))
                .ConfigureServices((context, services) =>
                {
                    services.AddRouting();
                    services.AddSharedJwtAuthentication(context.Configuration);
                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        tokens.TrustTestKey);
                    services.AddAuthorizationBuilder()
                        .SetAuthenticatedFallbackPolicy()
                        .AddPermissionPolicies(new HashSet<string> { InvitePermission });
                    services.AddActorContextMiddleware();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseActorContext();
                    app.UseEndpoints(endpoints => endpoints
                        .MapGet(InviteRoute, (ICurrentUserProvider provider) => provider.GetUserId().ToString())
                        .RequireAuthorization(InvitePermission));
                }))
            .StartAsync();

    private static IEnumerable<KeyValuePair<string, string?>> Settings =>
        TestTokenFactory.JwtSettings.Append(
            new KeyValuePair<string, string?>($"{ServiceActorOptions.SectionName}:AllowedClients:0", AllowedClient));
}
