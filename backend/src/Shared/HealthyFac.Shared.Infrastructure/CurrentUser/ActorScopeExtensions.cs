using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.CurrentUser;

public static class ActorScopeExtensions
{
    extension(IServiceScopeFactory scopeFactory)
    {
        /// <summary>
        /// Creates a scope that holds <paramref name="actor"/> until the scope ends. Use it as the only way to
        /// start a scope outside an HTTP request: a job, a processor step, or the database start-up. The scope
        /// then never runs without an actor, and no caller copies the actor setup.
        /// </summary>
        /// <param name="actor">The actor that the work of the scope runs as.</param>
        /// <returns>The new <see cref="IServiceScope"/>. The caller disposes it.</returns>
        public IServiceScope CreateActorScope(ActorContext actor)
        {
            var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ActorContextHolder>().Enter(actor);

            return scope;
        }
    }
}
