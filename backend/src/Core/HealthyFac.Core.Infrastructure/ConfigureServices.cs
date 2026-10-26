using Microsoft.EntityFrameworkCore;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.Infrastructure.Employees.InviteTokens;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Time;

namespace HealthyFac.Core.Infrastructure;

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
            services.AddScoped<AuditableEntityInterceptor>();

            var connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(DatabaseConstants.Errors.MissingConnectionString);
            }

            services.AddMessagingDbContext<ICoreDbContext, CoreDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                        DatabaseConstants.Retry.MaxCount,
                        TimeSpan.FromSeconds(DatabaseConstants.Retry.MaxDelaySeconds),
                        errorCodesToAdd: null));
                options.UseExceptionProcessor();

                options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());

                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddMessagingTransport(configuration);
            services.AddMessageProcessors();

            services.AddValidatedOptions<InviteTokenOptions, InviteTokenOptionsValidator>(
                InviteTokenOptions.SectionName, configuration);
            services.AddSingleton<IInviteTokenService, HmacInviteTokenService>();

            services.AddValidatedOptions<SeedingOptions, SeedingOptionsValidator>(
                SeedingOptions.SectionName, configuration);
            services.AddScoped<CoreDevelopmentDataSeeder>();
            services.AddScoped<ICoreDbInitializer, CoreDbInitializer>();

            return services;
        }
    }
}
