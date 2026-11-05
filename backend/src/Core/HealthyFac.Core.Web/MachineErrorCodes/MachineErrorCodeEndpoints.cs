using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCreateMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.CreateMachineErrorCodeRequest;
using ApplicationDeleteMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.DeleteMachineErrorCodeRequest;
using ApplicationGetMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.GetMachineErrorCodeRequest;
using ApplicationSearchMachineErrorCodesRequest =
    HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes.SearchMachineErrorCodesRequest;
using ApplicationUpdateMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.UpdateMachineErrorCodeRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

/// <summary>
/// Configures endpoint routing for the error codes of a machine.
/// </summary>
public static class MachineErrorCodeEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all machine error code endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMachineErrorCodeEndpoints()
        {
            var group = app.MapGroup(MachineErrorCodeRoutes.BaseRoute);
            group.MapPost(MachineErrorCodeRoutes.Root, CreateAsync)
                .RequireAuthorization(CorePermissions.MachineErrorCodes.Write);
            group.MapPost(MachineErrorCodeRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.MachineErrorCodes.Read);
            group.MapGet(MachineErrorCodeRoutes.ById, GetAsync)
                .RequireAuthorization(CorePermissions.MachineErrorCodes.Read);
            group.MapPut(MachineErrorCodeRoutes.ById, UpdateAsync)
                .RequireAuthorization(CorePermissions.MachineErrorCodes.Write);
            group.MapDelete(MachineErrorCodeRoutes.ById, DeleteAsync)
                .RequireAuthorization(CorePermissions.MachineErrorCodes.Delete);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        [FromRoute(Name = MachineErrorCodeRoutes.MachineIdParameter)] string machineId,
        CreateMachineErrorCodeRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateMachineErrorCodeRequest, CreateMachineErrorCodeResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        var location = string.Format(
            MachineErrorCodeRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<Ok<GridResponse<MachineErrorCodeResponse>>> SearchAsync(
        [FromRoute(Name = MachineErrorCodeRoutes.MachineIdParameter)] string machineId,
        SearchMachineErrorCodesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchMachineErrorCodesRequest, GridResponse<MachineErrorCodeResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<MachineErrorCodeResponse>> GetAsync(
        [AsParameters] GetMachineErrorCodeRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMachineErrorCodeRequest, MachineErrorCodeResponse> handler,
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
        [FromRoute(Name = MachineErrorCodeRoutes.MachineIdParameter)] string machineId,
        [FromRoute(Name = MachineErrorCodeRoutes.IdParameter)]
        string id,
        UpdateMachineErrorCodeRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateMachineErrorCodeRequest> handler,
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
        [AsParameters] DeleteMachineErrorCodeRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteMachineErrorCodeRequest> handler,
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
