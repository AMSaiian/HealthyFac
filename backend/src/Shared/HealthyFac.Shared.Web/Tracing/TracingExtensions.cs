using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Web.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HealthyFac.Shared.Web.Tracing;

/// <summary>
/// Provides extension methods for registering OpenTelemetry tracing.
/// </summary>
public static class TracingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the OpenTelemetry tracer. Registering it is what makes
        /// <c>ActivitySource.StartActivity</c> return a live activity, so the use case spans carry their own
        /// duration and every log entry gains a trace identifier.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedTracing(IConfiguration configuration)
        {
            services.AddValidatedOptions<TracingOptions, TracingOptionsValidator>(
                TracingOptions.SectionName,
                configuration);

            var options = configuration
                    .GetSection(TracingOptions.SectionName)
                    .Get<TracingOptions>()
                ?? new TracingOptions();

            if (!options.Enabled)
            {
                return services;
            }

            services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    if (!string.IsNullOrWhiteSpace(options.ServiceName))
                    {
                        resource.AddService(options.ServiceName);
                    }
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(ObservabilityConstants.Sources.UseCases)
                        .AddSource(ObservabilityConstants.Sources.IdEncryption)
                        .AddSource(options.AdditionalSources)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SamplingRatio)));

                    if (!string.IsNullOrWhiteSpace(options.Endpoint))
                    {
                        tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.Endpoint));
                    }
                });

            return services;
        }
    }
}
