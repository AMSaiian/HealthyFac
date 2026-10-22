using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Provides extension methods for enforcing transport security.
/// </summary>
public static class HttpsExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures HTTPS redirection and the HSTS policy from the transport security section.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedHttps(IConfiguration configuration)
        {
            services.AddValidatedOptions<HttpsOptions, HttpsOptionsValidator>(HttpsOptions.SectionName, configuration);

            var options = configuration
                    .GetSection(HttpsOptions.SectionName)
                    .Get<HttpsOptions>()
                ?? new HttpsOptions();

            services.AddSingleton(options);

            if (options.RedirectEnabled && options.RedirectPort is not null)
            {
                services.Configure<HttpsRedirectionOptions>(redirection =>
                    redirection.HttpsPort = options.RedirectPort);
            }

            services.AddHsts(hsts =>
            {
                hsts.MaxAge = TimeSpan.FromDays(options.HstsMaxAgeDays);
                hsts.IncludeSubDomains = options.HstsIncludeSubDomains;
                hsts.Preload = options.HstsPreload;
            });

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Applies HSTS outside development, then redirects plain HTTP when redirection is configured.
        /// Call this after the forwarded headers middleware, so the original scheme is already known.
        /// </summary>
        /// <returns>The <see cref="WebApplication"/> instance for chained configuration.</returns>
        public WebApplication UseSharedHttps()
        {
            var options = app.Services.GetRequiredService<HttpsOptions>();

            if (options.HstsEnabled && !app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            if (options.RedirectEnabled && options.RedirectPort is not null)
            {
                app.UseHttpsRedirection();
            }

            return app;
        }
    }
}
