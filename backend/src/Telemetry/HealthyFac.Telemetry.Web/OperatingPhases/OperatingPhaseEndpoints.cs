using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCreateOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.CreateOperatingPhaseRequest;
using ApplicationDeleteOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.DeleteOperatingPhaseRequest;
using ApplicationGetOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.GetOperatingPhaseRequest;
using ApplicationListOperatingPhasesRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.ListOperatingPhasesRequest;
using ApplicationReorderOperatingPhasesRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.ReorderOperatingPhasesRequest;
using ApplicationUpdateOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.UpdateOperatingPhaseRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

/// <summary>
/// Configures endpoint routing for the operating phases of a machine.
/// </summary>
public static class OperatingPhaseEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all operating phase endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapOperatingPhaseEndpoints()
        {
            var group = app.MapGroup(OperatingPhaseRoutes.BaseRoute);
            group.MapGet(OperatingPhaseRoutes.Root, ListAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Read);
            group.MapPost(OperatingPhaseRoutes.Root, CreateAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Write);
            group.MapPut(OperatingPhaseRoutes.Order, ReorderAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Write);
            group.MapGet(OperatingPhaseRoutes.ById, GetAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Read);
            group.MapPut(OperatingPhaseRoutes.ById, UpdateAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Write);
            group.MapDelete(OperatingPhaseRoutes.ById, DeleteAsync)
                .RequireAuthorization(TelemetryPermissions.OperatingPhases.Write);

            return app;
        }
    }

    private static async Task<Ok<ListOperatingPhasesResponse>> ListAsync(
        [AsParameters] ListOperatingPhasesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationListOperatingPhasesRequest, ListOperatingPhasesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> CreateAsync(
        [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)] string machineId,
        CreateOperatingPhaseRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateOperatingPhaseRequest, CreateOperatingPhaseResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        var location = string.Format(
            OperatingPhaseRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<NoContent> ReorderAsync(
        [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)] string machineId,
        ReorderOperatingPhasesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationReorderOperatingPhasesRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<OperatingPhaseResponse>> GetAsync(
        [AsParameters] GetOperatingPhaseRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetOperatingPhaseRequest, OperatingPhaseResponse> handler,
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
        [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)] string machineId,
        [FromRoute(Name = OperatingPhaseRoutes.IdParameter)]
        string id,
        UpdateOperatingPhaseRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateOperatingPhaseRequest> handler,
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
        [AsParameters] DeleteOperatingPhaseRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteOperatingPhaseRequest> handler,
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
