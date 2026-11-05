using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Database.Initialization;

/// <summary>
/// Provides the database initialization entry point for a host.
/// The extension targets <see cref="IServiceProvider"/> rather than a web type, so every host shape can
/// call it and the infrastructure layer needs no reference to the web stack.
/// </summary>
public static class DatabaseInitializationExtensions
{
    extension(IServiceProvider services)
    {
        /// <summary>
        /// Applies database migrations (Development only), seeds the required data, then seeds the optional
        /// data when the seeding section enables it. The scope runs as the database initialization actor of the
        /// service, so seeded rows carry its ID.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using var scope = services.GetRequiredService<IServiceScopeFactory>()
                .CreateActorScope(
                    ServiceActor.DatabaseInitialization.For(
                        services.GetRequiredService<IOptions<ServiceOptions>>().Value.ToServiceName()));

            var initializer = scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>();
            var seedingOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;

            await initializer.ApplyDatabaseStructureAsync(cancellationToken);
            await initializer.SeedRequiredDataAsync(cancellationToken);

            if (seedingOptions.IsOptionalDataSeeded)
            {
                await initializer.SeedOptionalDataAsync(cancellationToken);
            }
        }
    }
}
