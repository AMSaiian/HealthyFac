using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Application;

/// <summary>
/// Registers application layer services into the dependency injection container.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the use cases (handlers, decorators, and validators) and the services that they share.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApplicationServices()
        {
            services.AddUseCases(InsightsApplicationAssembly.Instance);
            services.AddScoped<IngestPipeline>();
            services.AddSingleton<IngestMetrics>();
            services.AddSingleton<DraftProposalMapper>();

            return services;
        }
    }
}
