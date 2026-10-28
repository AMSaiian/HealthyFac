using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Sets the actor of the request scope. An authenticated request runs as the user of its token, and an anonymous
/// request runs as the system actor. Run it after authentication and after the exception handling, so a token
/// with an invalid shape becomes <c>401</c>. The framework creates the middleware from the request scope, so it
/// receives the scoped actor holder through its constructor.
/// </summary>
/// <param name="holder">The holder of the actor of the request scope.</param>
internal sealed class ActorContextMiddleware(ActorContextHolder holder) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var actor = context.User.Identity?.IsAuthenticated == true
            ? ActorClaimsReader.Read(context.User)
            : SystemActor.Create();

        using var actorScope = holder.Enter(actor);

        await next(context);
    }
}
