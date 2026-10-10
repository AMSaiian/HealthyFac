using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Database;

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
        /// data when the seeding section enables it.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using var scope = services.CreateScope();

            var initializer = scope.ServiceProvider.GetRequiredService<IApplicationDbInitializer>();
            var seedingOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;

            await initializer.ApplyDatabaseStructureAsync(cancellationToken);
            await initializer.SeedRequiredDataAsync(cancellationToken);

            if (seedingOptions.SeedOptionalData)
            {
                await initializer.SeedOptionalDataAsync(cancellationToken);
            }
        }
    }
}
