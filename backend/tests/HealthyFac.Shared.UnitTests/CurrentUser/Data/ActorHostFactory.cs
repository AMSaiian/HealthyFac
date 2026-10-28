using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Web.CurrentUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

/// <summary>
/// Starts an in-memory host with the actor middleware. The host answers with the user ID that
/// <see cref="ICurrentUserProvider"/> reports inside the request.
/// </summary>
public static class ActorHostFactory
{
    public const string UserIdPath = "/user-id";

    public static Task<IHost> StartAsync(ClaimsPrincipal? user = null) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddActorContextMiddleware())
                .Configure(app =>
                {
                    if (user is not null)
                    {
                        app.Use((context, next) =>
                        {
                            context.User = user;

                            return next(context);
                        });
                    }

                    app.UseActorContext();
                    app.Run(context => context.Response.WriteAsync(
                        context.RequestServices.GetRequiredService<ICurrentUserProvider>().GetUserId().ToString()));
                }))
            .StartAsync();
}
