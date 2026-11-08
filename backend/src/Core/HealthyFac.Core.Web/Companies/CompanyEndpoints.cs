using HealthyFac.Core.Application.Companies;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthyFac.Core.Web.Companies;

/// <summary>
/// Configures endpoint routing for company operations.
/// </summary>
public static class CompanyEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all company endpoints to the application route builder. Any signed-in user may read the company,
        /// because it holds only the company of that user.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapCompanyEndpoints()
        {
            var group = app.MapGroup(CompanyRoutes.BaseRoute);
            group.MapGet(CompanyRoutes.Root, GetCurrentAsync);

            return app;
        }
    }

    private static async Task<Ok<GetCurrentCompanyResponse>> GetCurrentAsync(
        IRequestHandler<GetCurrentCompanyRequest, GetCurrentCompanyResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(new GetCurrentCompanyRequest(), cancellationToken);

        return TypedResults.Ok(response);
    }
}
