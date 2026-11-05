using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Web.CurrentUser.ServiceActors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Sets the actor of the request scope. An authenticated request runs as the user of its token, or as the actor
/// that the headers name when the token is a service token of an allowed client. A request without a signed-in user
/// runs as the anonymous actor, never as a service actor. Run it after authentication and
/// after the exception handling, so a token with an invalid shape becomes <c>401</c>. The framework creates the
/// middleware from the request scope, so it receives the scoped actor holder through its constructor.
/// </summary>
/// <param name="holder">The holder of the actor of the request scope.</param>
/// <param name="options">Lists the clients whose service tokens this service accepts.</param>
internal sealed class ActorContextMiddleware(ActorContextHolder holder, IOptions<ServiceActorOptions> options)
    : IMiddleware
{
    private readonly ActorContextHolder _holder = holder;
    private readonly ServiceActorOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var actor = context.User.Identity?.IsAuthenticated == true
            ? ActorClaimsReader.Read(context.User, context.Request.Headers, _options)
            : KnownActors.Anonymous;

        using var actorScope = _holder.Enter(actor);

        await next(context);
    }
}
