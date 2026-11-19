using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationChangeMachineStateRequest = HealthyFac.Core.Application.Machines.ChangeMachineStateRequest;
using ApplicationCreateMachineRequest = HealthyFac.Core.Application.Machines.CreateMachineRequest;
using ApplicationDeleteMachineRequest = HealthyFac.Core.Application.Machines.DeleteMachineRequest;
using ApplicationGetMachineStateTimesRequest = HealthyFac.Core.Application.Machines.GetMachineStateTimesRequest;
using ApplicationGetMachineRequest = HealthyFac.Core.Application.Machines.GetMachineRequest;
using ApplicationLookupMachineNamesRequest = HealthyFac.Core.Application.Machines.LookupMachineNamesRequest;
using ApplicationLookupMachinesRequest = HealthyFac.Core.Application.Machines.LookupMachinesRequest;
using ApplicationSearchMachinesRequest = HealthyFac.Core.Application.Machines.SearchMachines.SearchMachinesRequest;
using ApplicationUpdateMachineRequest = HealthyFac.Core.Application.Machines.UpdateMachineRequest;

namespace HealthyFac.Core.Web.Machines;

/// <summary>
/// Configures endpoint routing for machine business operations.
/// </summary>
public static class MachineEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all machine endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMachineEndpoints()
        {
            var group = app.MapGroup(MachineRoutes.BaseRoute);
            group.MapPost(MachineRoutes.Root, CreateAsync)
                .RequireAuthorization(CorePermissions.Machines.Write);
            group.MapPost(MachineRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.Machines.Read);
            group.MapPost(MachineRoutes.Names, LookupNamesAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            group.MapPost(MachineRoutes.Lookup, LookupAsync)
                .RequireAuthorization(CorePermissions.Machines.Read);
            group.MapGet(MachineRoutes.ById, GetAsync)
                .RequireAuthorization(CorePermissions.Machines.Read);
            group.MapGet(MachineRoutes.StateTimes, GetStateTimesAsync)
                .RequireAuthorization(CorePermissions.Machines.Read);
            group.MapPut(MachineRoutes.ById, UpdateAsync)
                .RequireAuthorization(CorePermissions.Machines.Write);
            group.MapPost(MachineRoutes.State, ChangeStateAsync)
                .RequireAuthorization(CorePermissions.Machines.Write);
            group.MapDelete(MachineRoutes.ById, DeleteAsync)
                .RequireAuthorization(CorePermissions.Machines.Delete);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateMachineRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateMachineRequest, CreateMachineResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        var location = string.Format(
            MachineRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<Ok<GridResponse<MachineResponse>>> SearchAsync(
        SearchMachinesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchMachinesRequest, GridResponse<MachineResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<LookupResponse>> LookupAsync(
        LookupMachinesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationLookupMachinesRequest, LookupResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<LookupMachineNamesResponse>> LookupNamesAsync(
        LookupMachineNamesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationLookupMachineNamesRequest, LookupMachineNamesResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<MachineStateTimesResponse>> GetStateTimesAsync(
        [AsParameters] GetMachineStateTimesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMachineStateTimesRequest, MachineStateTimesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<MachineResponse>> GetAsync(
        [AsParameters] GetMachineRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMachineRequest, MachineResponse> handler,
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
        [FromRoute(Name = MachineRoutes.IdParameter)] string id,
        UpdateMachineRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateMachineRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ChangeStateAsync(
        [FromRoute(Name = MachineRoutes.IdParameter)] string id,
        ChangeMachineStateRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationChangeMachineStateRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        [AsParameters] DeleteMachineRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteMachineRequest> handler,
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
