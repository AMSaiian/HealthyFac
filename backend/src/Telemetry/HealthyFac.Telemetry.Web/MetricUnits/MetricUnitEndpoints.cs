using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.MetricUnits;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthyFac.Telemetry.Web.MetricUnits;

/// <summary>
/// Configures endpoint routing for the unit catalog.
/// </summary>
public static class MetricUnitEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the unit catalog endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMetricUnitEndpoints()
        {
            app.MapGet(MetricUnitRoutes.BaseRoute, ListAsync)
                .RequireAuthorization(TelemetryPermissions.MetricDefinitions.Read);

            return app;
        }
    }

    private static async Task<Ok<ListMetricUnitsResponse>> ListAsync(
        IRequestHandler<ListMetricUnitsRequest, ListMetricUnitsResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(new ListMetricUnitsRequest(), cancellationToken);

        return TypedResults.Ok(response);
    }
}
