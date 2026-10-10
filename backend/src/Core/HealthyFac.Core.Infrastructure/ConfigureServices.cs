using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Application.Interfaces;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Providers;

namespace HealthyFac.Core.Infrastructure;

/// <summary>
/// Registers infrastructure layer services into the service collection.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers infrastructure database context, interceptors, and database initializers.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing database
        /// connection settings.</param>
        /// <param name="environment">The host environment deciding whether verbose database logging runs.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the database connection string is missing or blank.
        /// </exception>
        public IServiceCollection AddInfrastructureServices(
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            services.AddSingleton<IDateProvider, DateProvider>();
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<CoreDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            var connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(DatabaseConstants.Errors.MissingConnectionString);
            }

            services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                        DatabaseConstants.Retry.MaxCount,
                        TimeSpan.FromSeconds(DatabaseConstants.Retry.MaxDelaySeconds),
                        errorCodesToAdd: null));

                options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());

                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.Configure<SeedingOptions>(configuration.GetSection(SeedingOptions.SectionName));
            services.AddScoped<IApplicationDbInitializer, ApplicationDbInitializer>();

            return services;
        }
    }
}
