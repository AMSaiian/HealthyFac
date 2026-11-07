using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationApplyMetricDefinitionChangesRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.ApplyMetricDefinitionChangesRequest;
using ApplicationGetMetricSchemaRequest = HealthyFac.Telemetry.Application.MetricDefinitions.GetMetricSchemaRequest;
using ApplicationListMetricDefinitionsRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.ListMetricDefinitionsRequest;

namespace HealthyFac.Telemetry.Web.MetricDefinitions;

/// <summary>
/// Configures endpoint routing for the metric definitions of a machine.
/// </summary>
public static class MetricDefinitionEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all metric definition endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMetricDefinitionEndpoints()
        {
            app.MapGet(MetricDefinitionRoutes.DefinitionsRoute, ListAsync)
                .RequireAuthorization(TelemetryPermissions.MetricDefinitions.Read);
            app.MapPut(MetricDefinitionRoutes.DefinitionsRoute, ApplyAsync)
                .RequireAuthorization(TelemetryPermissions.MetricDefinitions.Write);
            app.MapGet(MetricDefinitionRoutes.SchemaRoute, GetSchemaAsync)
                .RequireAuthorization(TelemetryPermissions.MetricDefinitions.Read);

            return app;
        }
    }

    private static async Task<Ok<ListMetricDefinitionsResponse>> ListAsync(
        [AsParameters] ListMetricDefinitionsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationListMetricDefinitionsRequest, ListMetricDefinitionsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<ApplyMetricDefinitionChangesResponse>> ApplyAsync(
        [FromRoute(Name = MetricDefinitionRoutes.MachineIdParameter)] string machineId,
        ApplyMetricDefinitionChangesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationApplyMetricDefinitionChangesRequest, ApplyMetricDefinitionChangesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GetMetricSchemaResponse>> GetSchemaAsync(
        [AsParameters] GetMetricSchemaRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMetricSchemaRequest, GetMetricSchemaResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
