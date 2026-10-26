using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.CurrentUser;

public static class ActorContextExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the actor holder, the <c>ICurrentUserProvider</c>, and the scoped actor middleware. A
        /// middleware that implements <see cref="Microsoft.AspNetCore.Http.IMiddleware"/> needs this registration.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddActorContextMiddleware()
        {
            services.AddActorContext();
            services.AddScoped<ActorContextMiddleware>();

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Sets the actor of each request scope once, before the endpoints run. Register the services with
        /// <see cref="AddActorContextMiddleware"/>.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseActorContext()
        {
            app.UseMiddleware<ActorContextMiddleware>();

            return app;
        }
    }
}
