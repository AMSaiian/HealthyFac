using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.Infrastructure.Database;

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
        /// Applies database migrations (Development only). The scope runs as the system actor, as the scope of
        /// every start-up work does.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using var scope = services.GetRequiredService<IServiceScopeFactory>()
                .CreateActorScope(SystemActor.Create());

            var initializer = scope.ServiceProvider.GetRequiredService<IAuthDbInitializer>();

            await initializer.ApplyDatabaseStructureAsync(cancellationToken);
        }
    }
}
