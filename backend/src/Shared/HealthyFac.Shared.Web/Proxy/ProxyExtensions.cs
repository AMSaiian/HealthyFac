using HealthyFac.Shared.Infrastructure.Configuration;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.Proxy;

/// <summary>
/// Provides extension methods for reading client address and scheme from reverse proxy headers.
/// </summary>
public static class ProxyExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures the forwarded headers middleware from the proxy section.
        /// The defaults trust loopback only, so an explicit proxy or network list is required in a container.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedForwardedHeaders(IConfiguration configuration)
        {
            services.AddValidatedOptions<ProxyOptions, ProxyOptionsValidator>(ProxyOptions.SectionName, configuration);

            var proxyOptions = configuration
                    .GetSection(ProxyOptions.SectionName)
                    .Get<ProxyOptions>()
                ?? new ProxyOptions();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
                    | ForwardedHeaders.XForwardedHost;

                options.ForwardLimit = proxyOptions.ForwardLimit;

                if (proxyOptions.TrustAllProxies)
                {
                    options.KnownProxies.Clear();
                    options.KnownIPNetworks.Clear();
                    options.ForwardLimit = null;

                    return;
                }

                if (proxyOptions.KnownProxies.Length == 0 && proxyOptions.KnownNetworks.Length == 0)
                {
                    return;
                }

                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();

                foreach (var proxy in proxyOptions.KnownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }

                foreach (var network in proxyOptions.KnownNetworks)
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            });

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Applies the forwarded headers middleware. Call this first in the pipeline, so every later
        /// component and every log entry sees the real client address and scheme.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSharedForwardedHeaders()
            => app.UseForwardedHeaders();
    }
}
