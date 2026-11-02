using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Web.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace HealthyFac.Shared.Web.Logging;

/// <summary>
/// Provides extension methods to register and configure Serilog logging.
/// </summary>
public static class LoggingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Serilog as the logging provider for the whole host. Every framework source, including
        /// Entity Framework Core and ASP.NET Core, writes through this logger, so levels come from the
        /// Serilog section. The method attaches the OpenTelemetry exporter when an endpoint is configured.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <param name="configureLogger">
        /// Optional hook to customize the <see cref="LoggerConfiguration"/> after the shared setup runs.
        /// </param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedLogging(
            IConfiguration configuration,
            Action<LoggerConfiguration>? configureLogger = null
        )
        {
            services.AddValidatedOptions<OpenTelemetryLoggingOptions, OpenTelemetryLoggingOptionsValidator>(
                OpenTelemetryLoggingOptions.SectionName,
                configuration);

            var exporterOptions = configuration
                .GetSection(OpenTelemetryLoggingOptions.SectionName)
                .Get<OpenTelemetryLoggingOptions>();

            services.AddSerilog((provider, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Destructure.With<SensitiveDataDestructuringPolicy>();

                ApplyOpenTelemetryExporter(loggerConfiguration, exporterOptions, provider, configuration);

                configureLogger?.Invoke(loggerConfiguration);
            });

            return services;
        }
    }

    /// <summary>
    /// Attaches the OTLP sink when the options enable it and name an endpoint.
    /// Serilog stamps every log event with the current trace and span identifiers, so the exported logs
    /// correlate with the use case activities without an extra enricher.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to extend.</param>
    /// <remarks>
    /// The sink carries the shared resource attributes (<see cref="TelemetryResource"/>), so a log line joins the
    /// trace and the metrics of the same instance. The sink reads the <c>OTEL_*</c> variables through
    /// <paramref name="configuration"/> and lets them override the configured values, as the SDK detector does.
    /// </remarks>
    /// <param name="loggerConfiguration">The logger configuration to extend.</param>
    /// <param name="options">The exporter options, or <see langword="null"/> when the section is absent.</param>
    /// <param name="provider">The service provider that holds the inputs of the resource attributes.</param>
    /// <param name="configuration">The application configuration, which includes the environment variables.</param>
    private static void ApplyOpenTelemetryExporter(
        LoggerConfiguration loggerConfiguration,
        OpenTelemetryLoggingOptions? options,
        IServiceProvider provider,
        IConfiguration configuration
    )
    {
        if (options is null || !options.Enabled || string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return;
        }

        loggerConfiguration.WriteTo.OpenTelemetry(
            sink =>
            {
                sink.Endpoint = options.Endpoint;
                sink.Protocol = OtlpProtocol.HttpProtobuf;
                sink.IncludedData = IncludedData.TraceIdField
                    | IncludedData.SpanIdField
                    | IncludedData.MessageTemplateTextAttribute
                    | IncludedData.SourceContextAttribute;
                sink.ResourceAttributes = TelemetryResource.CreateAttributes(provider).ToDictionary();
            },
            key => configuration[key]);
    }
}
