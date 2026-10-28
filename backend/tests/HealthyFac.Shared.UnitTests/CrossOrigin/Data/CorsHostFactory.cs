using HealthyFac.Shared.UnitTests.HealthChecks.Data;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CrossOrigin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.CrossOrigin.Data;

/// <summary>
/// Starts an in-memory host with the shared CORS policy and a fallback policy that needs a signed-in user, as Core
/// has. One endpoint asks for the policy, and one does not.
/// </summary>
public static class CorsHostFactory
{
    public const string AllowedOrigin = "http://app.healthyfac.test:4200";
    public const string OtherOrigin = "http://other.test";
    public const string EndpointWithPolicy = "/with-cors";
    public const string EndpointWithoutPolicy = "/without-cors";
    public const int PreflightMaxAgeSeconds = 600;

    private const string DenyScheme = "deny";

    public static Task<IHost> StartAsync() =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{CorsPolicyOptions.SectionName}:Enabled"] = bool.TrueString,
                        [$"{CorsPolicyOptions.SectionName}:AllowedOrigins:0"] = AllowedOrigin,
                        [$"{CorsPolicyOptions.SectionName}:PreflightMaxAgeSeconds"] =
                            PreflightMaxAgeSeconds.ToString(),
                    }))
                .ConfigureServices((context, services) =>
                {
                    services.AddRouting();
                    services.AddSharedCors(context.Configuration);
                    services.AddAuthentication(DenyScheme)
                        .AddScheme<AuthenticationSchemeOptions, DenyHandler>(DenyScheme, null);
                    services.AddAuthorizationBuilder()
                        .SetAuthenticatedFallbackPolicy();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseSharedCors();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost(EndpointWithPolicy, () => Results.Ok())
                            .RequireCors(CorsConstants.PolicyName);
                        endpoints.MapPost(EndpointWithoutPolicy, () => Results.Ok());
                    });
                }))
            .StartAsync();
}
