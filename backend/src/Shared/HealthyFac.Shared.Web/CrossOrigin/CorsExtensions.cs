using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.CrossOrigin;

/// <summary>
/// Provides extension methods for browser cross-origin access.
/// </summary>
public static class CorsExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the shared cross-origin policy when the configuration enables it.
        /// Registration is skipped entirely while a gateway owns the policy, so this service emits no
        /// cross-origin headers and cannot duplicate the gateway's.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedCors(IConfiguration configuration)
        {
            services.AddValidatedOptions<CorsPolicyOptions, CorsPolicyOptionsValidator>(
                CorsPolicyOptions.SectionName,
                configuration);

            var options = configuration
                .GetSection(CorsPolicyOptions.SectionName)
                .Get<CorsPolicyOptions>();

            if (options is null || !options.Enabled)
            {
                return services;
            }

            var allowsWildcard = options.AllowedOrigins.Contains(CorsConstants.Wildcard);

            services.AddCors(cors => cors.AddPolicy(
                CorsConstants.PolicyName,
                policy =>
                {
                    if (allowsWildcard)
                    {
                        policy.AllowAnyOrigin();
                    }
                    else
                    {
                        policy.WithOrigins(options.AllowedOrigins);
                    }

                    policy.AllowAnyHeader();
                    policy.AllowAnyMethod();

                    if (options.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                }));

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Applies the shared cross-origin policy when it was registered. Call this after the forwarded
        /// headers middleware and before authorization.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSharedCors()
        {
            var policyProvider = app.ApplicationServices
                .GetService<ICorsService>();

            return policyProvider is null ? app : app.UseCors(CorsConstants.PolicyName);
        }
    }
}
