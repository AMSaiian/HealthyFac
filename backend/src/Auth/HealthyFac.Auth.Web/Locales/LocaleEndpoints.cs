using HealthyFac.Shared.Application.UseCases;
using ApplicationSetMyLocaleRequest = HealthyFac.Auth.Application.Locales.SetMyLocaleRequest;

namespace HealthyFac.Auth.Web.Locales;

/// <summary>
/// Configures endpoint routing for the locale of the current user.
/// </summary>
public static class LocaleEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all locale endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapLocaleEndpoints()
        {
            var group = app.MapGroup(LocaleRoutes.BaseRoute);
            group.MapPut(LocaleRoutes.Locale, SetAsync);

            return app;
        }
    }

    private static async Task<IResult> SetAsync(
        ApplicationSetMyLocaleRequest request,
        IRequestHandler<ApplicationSetMyLocaleRequest> handler,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(request, cancellationToken);

        return Results.NoContent();
    }
}
