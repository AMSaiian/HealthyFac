using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Provides extension methods for registering the ingest worker.
/// </summary>
public static class IngestWorkerExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated ingest options, the retry pipeline for external calls, the blob stream source, the
        /// claim queries, and the worker. The worker starts with the host unless <c>Ingest:IsWorkerEnabled</c> is
        /// false.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddIngestWorker(IConfiguration configuration)
        {
            services.AddValidatedOptions<IngestProcessingOptions, IngestProcessingOptionsValidator>(
                IngestProcessingOptions.SectionName,
                configuration);

            services.AddHttpClient(
                HttpBlobStreamSource.ClientName,
                client => client.Timeout = Timeout.InfiniteTimeSpan);
            services.AddSingleton<IBlobStreamSource, HttpBlobStreamSource>();
            services.AddSingleton<IProviderCalls, ProviderResilience>();
            services.AddScoped<IngestAttemptClaims>();
            services.AddSingleton<IngestWorker>();
            services.AddHostedService(provider => provider.GetRequiredService<IngestWorker>());
            services.AddSingleton<IngestBacklogSampler>();
            services.AddHostedService(provider => provider.GetRequiredService<IngestBacklogSampler>());

            return services;
        }
    }
}
