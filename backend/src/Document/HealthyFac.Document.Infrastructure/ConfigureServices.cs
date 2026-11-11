using EntityFramework.Exceptions.PostgreSQL;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Application;
using HealthyFac.Document.Application.Files.Inspection;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database.Initialization;
using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Document.Infrastructure.Files.Inspection;
using HealthyFac.Document.Infrastructure.Files.Links;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Shared.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Document.Infrastructure;

/// <summary>
/// Registers infrastructure layer services into the service collection.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers infrastructure database context, interceptors, database initializers, and the message
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

            services.AddMessagingDbContext<IDocumentDbContext, DocumentDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
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

            services.AddIntegrationMessage<MachineCreated>();
            services.AddIntegrationMessage<MachineDeleted>();
            services.AddIntegrationMessage<MachineRenamed>();
            services.AddIntegrationMessage<MachineDraftApplied>();
            services.AddIntegrationMessage<FileMachineMatched>();
            services.AddIntegrationMessage<IngestStateChanged>();

            services.AddValidatedOptions<SeedingOptions, SeedingOptionsValidator>(
                SeedingOptions.SectionName, configuration);

            services.AddFileStorage(configuration);
            services.AddSingleton<IFileInspector, FileSignaturesFileInspector>();
            services.AddSingleton<IFileLinkIssuer, SasFileLinkIssuer>();

            services.AddScoped<DocumentDevelopmentDataSeeder>();
            services.AddSingleton(DemoIngestSeedLocation.FromApplicationBase());
            services.AddSingleton<DemoFileSeeder>();
            services.AddScoped<IDocumentDbInitializer, DocumentDbInitializer>();

            return services;
        }
    }
}
