using Microsoft.EntityFrameworkCore;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthyFac.Auth.Application;
using HealthyFac.Auth.Application.Identities.ProvisionIdentity;
using HealthyFac.Auth.Application.Identities.RevokeIdentity;
using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Auth.Infrastructure.Keycloak;
using HealthyFac.Auth.Infrastructure.Revocation;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Time;

namespace HealthyFac.Auth.Infrastructure;

/// <summary>
/// Registers infrastructure layer services into the service collection.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers infrastructure database context, database initializer, the Keycloak admin client, and the
        /// message runtime: the RabbitMQ transport and the outbox and inbox processors.
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

            var connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(DatabaseConstants.Errors.MissingConnectionString);
            }

            services.AddMessagingDbContext<IAuthDbContext, AuthDbContext>((_, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                            DatabaseConstants.Retry.MaxCount,
                            TimeSpan.FromSeconds(DatabaseConstants.Retry.MaxDelaySeconds),
                            null)
                        .ConfigureParentOnlyTracing());
                options.UseExceptionProcessor();

                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddServiceOptions(configuration);
            services.AddMessagingTransport(configuration);
            services.AddMessageProcessors();
            services.AddIntegrationMessage<EmployeeAccepted>();
            services.AddIntegrationMessage<EmployeeDeactivated>();

            services.AddKeycloakIdentityAdmin(configuration);
            services.AddSessionRevocation(configuration);

            services.AddScoped<IAuthDbInitializer, AuthDbInitializer>();

            return services;
        }
    }
}
