using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCreateStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.CreateStatusDefinitionRequest;
using ApplicationDeleteStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.DeleteStatusDefinitionRequest;
using ApplicationGetStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.GetStatusDefinitionRequest;
using ApplicationSearchStatusDefinitionsRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions.SearchStatusDefinitionsRequest;
using ApplicationUpdateStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.UpdateStatusDefinitionRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

/// <summary>
/// Configures endpoint routing for the status definitions of a machine.
/// </summary>
public static class StatusDefinitionEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all status definition endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapStatusDefinitionEndpoints()
        {
            var group = app.MapGroup(StatusDefinitionRoutes.BaseRoute);
            group.MapPost(StatusDefinitionRoutes.Root, CreateAsync)
                .RequireAuthorization(TelemetryPermissions.StatusDefinitions.Write);
            group.MapPost(StatusDefinitionRoutes.Search, SearchAsync)
                .RequireAuthorization(TelemetryPermissions.StatusDefinitions.Read);
            group.MapGet(StatusDefinitionRoutes.ById, GetAsync)
                .RequireAuthorization(TelemetryPermissions.StatusDefinitions.Read);
            group.MapPut(StatusDefinitionRoutes.ById, UpdateAsync)
                .RequireAuthorization(TelemetryPermissions.StatusDefinitions.Write);
            group.MapDelete(StatusDefinitionRoutes.ById, DeleteAsync)
                .RequireAuthorization(TelemetryPermissions.StatusDefinitions.Delete);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        [FromRoute(Name = StatusDefinitionRoutes.MachineIdParameter)] string machineId,
        CreateStatusDefinitionRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateStatusDefinitionRequest, CreateStatusDefinitionResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        var location = string.Format(
            StatusDefinitionRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<Ok<GridResponse<StatusDefinitionResponse>>> SearchAsync(
        [FromRoute(Name = StatusDefinitionRoutes.MachineIdParameter)] string machineId,
        SearchStatusDefinitionsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchStatusDefinitionsRequest, GridResponse<StatusDefinitionResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<StatusDefinitionResponse>> GetAsync(
        [AsParameters] GetStatusDefinitionRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetStatusDefinitionRequest, StatusDefinitionResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> UpdateAsync(
        [FromRoute(Name = StatusDefinitionRoutes.MachineIdParameter)] string machineId,
        [FromRoute(Name = StatusDefinitionRoutes.IdParameter)]
        string id,
        UpdateStatusDefinitionRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateStatusDefinitionRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(machineId, id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        [AsParameters] DeleteStatusDefinitionRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteStatusDefinitionRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
