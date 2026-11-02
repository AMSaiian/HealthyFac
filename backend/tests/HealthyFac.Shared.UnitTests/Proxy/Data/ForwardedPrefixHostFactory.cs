using System.Net;
using HealthyFac.Shared.Web.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.Proxy.Data;

/// <summary>
/// Starts an in-memory host with the shared forwarded headers middleware. The host answers with the
/// <c>PathBase</c> that the middleware produced, and it trusts one network for forwarded headers.
/// </summary>
public static class ForwardedPrefixHostFactory
{
    public const string TrustedNetwork = "10.0.0.0/8";
    public const string TrustedClientAddress = "10.1.2.3";
    public const string UntrustedClientAddress = "203.0.113.9";
    public const string ForwardedPrefixHeader = "X-Forwarded-Prefix";

    public static Task<IHost> StartAsync(string clientAddress) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddSharedForwardedHeaders(BuildConfiguration()))
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse(clientAddress);

                        return next();
                    });
                    app.UseSharedForwardedHeaders();
                    app.Run(context => context.Response.WriteAsync(context.Request.PathBase.Value ?? string.Empty));
                }))
            .StartAsync();

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{ProxyOptions.SectionName}:KnownNetworks:0"] = TrustedNetwork,
                })
            .Build();
}
