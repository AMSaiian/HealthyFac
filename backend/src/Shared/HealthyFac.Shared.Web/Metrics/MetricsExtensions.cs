using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Web.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace HealthyFac.Shared.Web.Metrics;

/// <summary>
/// Provides extension methods for registering OpenTelemetry metrics.
/// </summary>
public static class MetricsExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OpenTelemetry meter provider with the shared resource of the traces and the logs. It listens
        /// to the messaging meter, the .NET runtime meter, the built-in ASP.NET Core and <c>HttpClient</c> meters
        /// (through the instrumentation packages of the tracer), and <see cref="MetricsOptions.AdditionalMeters"/>.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedMetrics(IConfiguration configuration)
        {
            services.AddValidatedOptions<MetricsOptions, MetricsOptionsValidator>(
                MetricsOptions.SectionName,
                configuration);

            var options = configuration
                    .GetSection(MetricsOptions.SectionName)
                    .Get<MetricsOptions>()
                ?? new MetricsOptions();

            if (!options.IsEnabled)
            {
                return services;
            }

            services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .ConfigureResource(resource => resource.AddSharedAttributes())
                        .AddMeter(ObservabilityConstants.Meters.Messaging)
                        .AddMeter(ObservabilityConstants.Meters.Runtime)
                        .AddMeter(options.AdditionalMeters)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    if (!string.IsNullOrWhiteSpace(options.Endpoint))
                    {
                        metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.Endpoint));
                    }
                });

            return services;
        }
    }
}
