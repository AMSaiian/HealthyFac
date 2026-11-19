using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Provides extension methods for registering the Core HTTP client.
/// </summary>
public static class CoreClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Core client options, the named HTTP client with the service token and the actor
        /// headers, and the work order reader. The service must call <c>AddServiceTokens</c>.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddCoreClient(IConfiguration configuration)
        {
            services.AddValidatedOptions<CoreClientOptions, CoreClientOptionsValidator>(
                CoreClientOptions.SectionName,
                configuration);

            services.AddHttpClient(
                    CoreClientNames.Client,
                    (provider, client) =>
                        client.BaseAddress = new Uri(
                            provider.GetRequiredService<IOptions<CoreClientOptions>>().Value.BaseAddress))
                .AddClientCredentialsToken()
                .AddActorHeaders();

            services.AddScoped<IWorkOrderCatalog, CoreWorkOrderCatalog>();

            return services;
        }
    }
}
