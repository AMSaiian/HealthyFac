using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Provides extension methods for the Client Credentials token of a service that calls another API for itself.
/// </summary>
public static class ServiceTokenExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated token options, the client of the token endpoint, the token cache, and the
        /// handler. Call it once for each service. A client then gets the token with
        /// <see cref="AddClientCredentialsToken"/>.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddServiceTokens(IConfiguration configuration)
        {
            services.AddSharedTime();
            services.AddValidatedOptions<ServiceTokenOptions, ServiceTokenOptionsValidator>(
                ServiceTokenOptions.SectionName,
                configuration);
            services.AddHttpClient(ServiceTokenConstants.TokenClientName);
            services.TryAddSingleton<ServiceTokenCache>();
            services.TryAddTransient<ClientCredentialsHandler>();

            return services;
        }
    }

    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Adds the handler that sets the access token of the service on each request of the client. The service
        /// must call <see cref="AddServiceTokens"/> first.
        /// </summary>
        /// <returns>The <see cref="IHttpClientBuilder"/> instance for chained configuration.</returns>
        public IHttpClientBuilder AddClientCredentialsToken()
            => builder.AddHttpMessageHandler<ClientCredentialsHandler>();
    }
}
