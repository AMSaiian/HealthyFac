using EntityFramework.Exceptions.PostgreSQL;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Conversion;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.Infrastructure.Database.Initialization;
using HealthyFac.Insights.Infrastructure.Database.Seeding;
using HealthyFac.Insights.Infrastructure.Extraction;
using HealthyFac.Insights.Infrastructure.Files;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.Infrastructure.MetricUnits;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.Infrastructure.Time;
using HealthyFac.Telemetry.Messaging.MetricUnits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Insights.Infrastructure;

/// <summary>
/// Registers infrastructure layer services into the service collection.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers infrastructure database context, interceptors, database initializer, and the message
        /// runtime: the RabbitMQ transport and the outbox and inbox processors.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing database
        /// and broker connection settings.</param>
        /// <param name="environment">The host environment deciding whether verbose database logging runs.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the database or the RabbitMQ connection string is missing or blank.
        /// </exception>
        public IServiceCollection AddInfrastructureServices(
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            services.AddSharedTime();
            services.AddSharedRichText();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<AuditableEntityInterceptor>();

            var connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(DatabaseConstants.Errors.MissingConnectionString);
            }

            services.AddMessagingDbContext<IInsightsDbContext, InsightsDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.UseVector()
                        .EnableRetryOnFailure(
                            DatabaseConstants.Retry.MaxCount,
                            TimeSpan.FromSeconds(DatabaseConstants.Retry.MaxDelaySeconds),
                            errorCodesToAdd: null)
                        .ConfigureParentOnlyTracing());
                options.UseExceptionProcessor();

                options.AddInterceptors(
                    provider.GetRequiredService<SoftDeleteInterceptor>(),
                    provider.GetRequiredService<AuditableEntityInterceptor>());

                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddServiceOptions(configuration);
            services.AddMessagingTransport(configuration);
            services.AddMessageProcessors();

            services.AddValidatedOptions<SeedingOptions, SeedingOptionsValidator>(
                SeedingOptions.SectionName, configuration);
            services.AddSingleton(DemoIngestSeedLocation.FromApplicationBase());
            services.AddSingleton<DemoIngestSeeder>();
            services.AddScoped<IInsightsDbInitializer, InsightsDbInitializer>();
            services.AddScoped<IIngestAttemptLock, IngestAttemptLock>();
            services.AddScoped<IMachineSignalCatalog, TelemetryMachineSignalCatalog>();
            services.AddSingleton<IDraftExtractor, AgentDraftExtractor>();
            services.AddIngestWorker(configuration);

            services.AddIntegrationMessage<FileDeleted>();
            services.AddIntegrationMessage<FileMachineChanged>();
            services.AddIntegrationMessage<MetricUnitsChanged>();

            services.AddSharedRedis(configuration);
            services.AddServiceTokens(configuration);
            services.AddTelemetryClient(configuration);
            services.AddDoclingConverter(configuration);
            services.AddGeminiClients(configuration);
            services.AddMetricUnitCatalog(configuration);
            services.AddDocumentClients(configuration);

            return services;
        }
    }
}
