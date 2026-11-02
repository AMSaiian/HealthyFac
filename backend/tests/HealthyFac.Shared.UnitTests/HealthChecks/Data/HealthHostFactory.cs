using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.HealthChecks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.HealthChecks.Data;

/// <summary>
/// Starts an in-memory host with the shared health endpoints and one ready check. The host with a fallback policy
/// behaves like Core: every endpoint needs a signed-in user unless it opts out. The host without it behaves like the
/// gateway.
/// </summary>
public static class HealthHostFactory
{
    public const int HealthPort = 8090;
    public const int PublicPort = 8080;
    public const string ReadyCheckName = "sample";
    public const string PingPath = "/ping";

    private const string DenyScheme = "deny";

    public static Task<IHost> StartAsync(bool hasFallbackPolicy) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{HealthEndpointOptions.SectionName}:Port"] = HealthPort.ToString(),
                    }))
                .ConfigureServices((context, services) =>
                {
                    services.AddRouting();
                    services.AddSharedHealthChecks(context.Configuration)
                        .AddCheck(ReadyCheckName, () => HealthCheckResult.Healthy(), [HealthCheckConstants.ReadyTag]);

                    if (hasFallbackPolicy)
                    {
                        services.AddAuthentication(DenyScheme)
                            .AddScheme<AuthenticationSchemeOptions, DenyHandler>(DenyScheme, null);
                        services.AddAuthorizationBuilder()
                            .SetAuthenticatedFallbackPolicy();
                    }
                })
                .Configure(app =>
                {
                    app.UseSharedHealthChecks();
                    app.UseRouting();

                    if (hasFallbackPolicy)
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();
                    }

                    app.UseEndpoints(endpoints => endpoints.MapGet(PingPath, () => Results.Ok()));
                }))
            .StartAsync();
}
