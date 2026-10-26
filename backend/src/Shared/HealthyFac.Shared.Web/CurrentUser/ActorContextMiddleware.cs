using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Sets the actor of the request scope. No authentication exists yet, so every request runs as the system actor.
/// The framework creates the middleware from the request scope, so it receives the scoped actor holder through
/// its constructor.
/// </summary>
/// <param name="holder">The holder of the actor of the request scope.</param>
internal sealed class ActorContextMiddleware(ActorContextHolder holder) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using var actorScope = holder.Enter(SystemActor.Create());

        await next(context);
    }
}
