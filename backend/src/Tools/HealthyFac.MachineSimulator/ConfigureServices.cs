using HealthyFac.Core.Application;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.MachineSimulator.Control;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Machines.Snapshots;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Simulation;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.Time;
using HealthyFac.Shared.Web.ErrorHandling;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.OpenApi;
using HealthyFac.Shared.Web.Serialization;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace HealthyFac.MachineSimulator;

/// <summary>
/// Configures dependency injection, the middleware pipeline, and the endpoints of the machine simulator. The tool
/// binds to the loopback address of a developer machine, so it has no authentication and no authorization.
/// </summary>
public static class ConfigureServices
{
    /// <summary>The service name of the telemetry resource. It is no member of the service identities.</summary>
    private const string ServiceName = "machine-simulator";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the options, the read-only database access, the time, the randomness, and the API services.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <param name="environment">The host environment.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSimulatorServices(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddSimulatorOptions(configuration);
            services.AddSimulatorDatabases(configuration, environment);
            services.AddSharedTime();
            services.AddSingleton(provider =>
                provider.GetRequiredService<IOptions<SimulatorOptions>>().Value.RandomSeed is { } seed
                    ? new Random(seed)
                    : Random.Shared);
            services.AddSimulation();
            services.AddSimulatorApi();
            services.AddSimulatorObservability(configuration);

            return services;
        }

        private IServiceCollection AddSimulatorOptions(IConfiguration configuration)
        {
            services.AddValidatedOptions<SimulatorOptions, SimulatorOptionsValidator>(
                SimulatorOptions.SectionName,
                configuration);
            services.AddValidatedOptions<SimulatorMqttOptions, SimulatorMqttOptionsValidator>(
                SimulatorMqttOptions.SectionName,
                configuration);
            services.AddSingleton<IOptions<ServiceOptions>>(Options.Create(new ServiceOptions { Name = ServiceName }));

            return services;
        }

        private IServiceCollection AddSimulatorDatabases(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddDbContext<CoreDbContext>(options => ConfigureReadOnly(
                options, configuration, SimulatorDatabaseConstants.ConnectionStrings.Core));
            services.AddScoped<ICoreDbContext>(provider => provider.GetRequiredService<CoreDbContext>());

            services.AddDbContext<TelemetryDbContext>(options => ConfigureReadOnly(
                options, configuration, SimulatorDatabaseConstants.ConnectionStrings.Telemetry));
            services.AddScoped<ITelemetryDbContext>(provider => provider.GetRequiredService<TelemetryDbContext>());

            return services;
        }

        private IServiceCollection AddSimulation()
        {
            services.AddHostedService<SimulatorInstanceLock>();
            services.AddScoped<MachineSnapshotReader>();
            services.AddScoped<CounterReadingReader>();
            services.AddSingleton<SimulationEngine>();
            services.AddSingleton<SimulatorMetrics>();
            services.AddSingleton<MqttMachineMessagePublisher>();
            services.AddSingleton<IMachineMessagePublisher>(provider =>
                provider.GetRequiredService<MqttMachineMessagePublisher>());
            services.AddHostedService(provider => provider.GetRequiredService<MqttMachineMessagePublisher>());
            services.AddHostedService<SimulationLoops>();

            return services;
        }

        private IServiceCollection AddSimulatorApi()
        {
            services.AddSharedJsonOptions();
            services.AddSharedExceptionHandling();
            services.AddSharedOpenApi();

            return services;
        }

        private IServiceCollection AddSimulatorObservability(IConfiguration configuration)
        {
            services.AddSharedLogging(configuration);
            services.AddSharedTracing(configuration);
            services.AddSharedMetrics(configuration);
            services.ConfigureOpenTelemetryTracerProvider(tracing =>
                tracing.AddSource(ObservabilityConstants.Simulator.Source));
            services.ConfigureOpenTelemetryMeterProvider(metrics =>
                metrics.AddMeter(ObservabilityConstants.Simulator.Meter));

            services.AddSharedHealthChecks(configuration)
                .AddDbContextCheck<CoreDbContext>(
                    SimulatorDatabaseConstants.HealthChecks.Core,
                    tags: [HealthCheckConstants.ReadyTag])
                .AddDbContextCheck<TelemetryDbContext>(
                    SimulatorDatabaseConstants.HealthChecks.Telemetry,
                    tags: [HealthCheckConstants.ReadyTag])
                .AddCheck<MqttConnectionHealthCheck>(
                    MqttConnectionHealthCheck.Name,
                    HealthStatus.Unhealthy,
                    [HealthCheckConstants.ReadyTag]);

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures the HTTP request pipeline. The health middleware comes first, then the request log wraps the
        /// exception handler, so the log records the status that the client received.
        /// </summary>
        /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
        public WebApplication UseSimulatorPipeline()
        {
            app.UseSharedHealthChecks();
            app.UseSharedRequestLogging();
            app.UseSharedExceptionHandling();

            return app;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the OpenAPI document and the Scalar dashboard in every environment, because the tool is internal.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSimulatorEndpoints()
        {
            app.MapControlEndpoints();
            app.MapOpenApi(OpenApiConstants.DocumentRoute);
            app.MapScalarApiReference(OpenApiConstants.DashboardRoute);

            return app;
        }
    }

    private static void ConfigureReadOnly(DbContextOptionsBuilder options, IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(SimulatorErrors.MissingConnectionString(name));
        }

        options.UseNpgsql(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
