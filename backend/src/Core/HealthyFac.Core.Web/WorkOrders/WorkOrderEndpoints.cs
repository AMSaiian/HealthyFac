using System.Security.Claims;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationAssignWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.AssignWorkOrderRequest;
using ApplicationChangeWorkOrderStatusRequest = HealthyFac.Core.Application.WorkOrders.ChangeWorkOrderStatusRequest;
using ApplicationCreateWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.CreateWorkOrderRequest;
using ApplicationDeleteWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.DeleteWorkOrderRequest;
using ApplicationGetWorkOrderHistoryRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderHistoryRequest;
using ApplicationGetWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderRequest;
using ApplicationMoveWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.MoveWorkOrderRequest;
using ApplicationSearchWorkOrderBatchRequest =
    HealthyFac.Core.Application.WorkOrders.SearchWorkOrders.SearchWorkOrderBatchRequest;
using ApplicationSearchWorkOrdersRequest =
    HealthyFac.Core.Application.WorkOrders.SearchWorkOrders.SearchWorkOrdersRequest;
using ApplicationUnassignWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.UnassignWorkOrderRequest;
using ApplicationUpdateWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.UpdateWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

/// <summary>
/// Configures endpoint routing for work orders. The scoped endpoints need <c>work-orders:read</c> or
/// <c>work-orders:write</c> and answer by the access scope of the token. The schedule endpoints (assign, move,
/// unassign, delete) need <c>work-orders:manage</c>.
/// </summary>
public static class WorkOrderEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all work order endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapWorkOrderEndpoints()
        {
            var group = app.MapGroup(WorkOrderRoutes.BaseRoute);
            group.MapPost(WorkOrderRoutes.Root, CreateAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Write);
            group.MapPost(WorkOrderRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Read);
            group.MapPost(WorkOrderRoutes.SearchBatch, SearchBatchAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Read);
            group.MapGet(WorkOrderRoutes.ById, GetAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Read);
            group.MapPut(WorkOrderRoutes.ById, UpdateAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Write);
            group.MapPost(WorkOrderRoutes.Status, ChangeStatusAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Write);
            group.MapGet(WorkOrderRoutes.History, GetHistoryAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Read);
            group.MapPut(WorkOrderRoutes.Assignee, AssignAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Manage);
            group.MapPost(WorkOrderRoutes.Move, MoveAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Manage);
            group.MapDelete(WorkOrderRoutes.Assignee, UnassignAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Manage);
            group.MapDelete(WorkOrderRoutes.ById, DeleteAsync)
                .RequireAuthorization(CorePermissions.WorkOrders.Manage);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkOrderRequest request,
        HttpRequest httpRequest,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateWorkOrderRequest, CreateWorkOrderResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage)),
            cancellationToken);

        var location = string.Format(
            WorkOrderRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<Ok<GridResponse<WorkOrderRowResponse>>> SearchAsync(
        SearchWorkOrdersRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchWorkOrdersRequest, GridResponse<WorkOrderRowResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage)),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<SearchWorkOrderBatchResponse>> SearchBatchAsync(
        SearchWorkOrderBatchRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchWorkOrderBatchRequest, SearchWorkOrderBatchResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage)),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<WorkOrderResponse>> GetAsync(
        [AsParameters] GetWorkOrderRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetWorkOrderRequest, WorkOrderResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> UpdateAsync(
        [FromRoute(Name = WorkOrderRoutes.IdParameter)] string id,
        UpdateWorkOrderRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateWorkOrderRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(
                id,
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ChangeStatusAsync(
        [FromRoute(Name = WorkOrderRoutes.IdParameter)] string id,
        ChangeWorkOrderStatusRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationChangeWorkOrderStatusRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(
                id,
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<WorkOrderHistoryResponse>>> GetHistoryAsync(
        [AsParameters] GetWorkOrderHistoryRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetWorkOrderHistoryRequest, IReadOnlyList<WorkOrderHistoryResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> AssignAsync(
        [FromRoute(Name = WorkOrderRoutes.IdParameter)] string id,
        AssignWorkOrderRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationAssignWorkOrderRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MoveAsync(
        [FromRoute(Name = WorkOrderRoutes.IdParameter)] string id,
        MoveWorkOrderRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationMoveWorkOrderRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnassignAsync(
        [AsParameters] UnassignWorkOrderRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUnassignWorkOrderRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        [AsParameters] DeleteWorkOrderRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteWorkOrderRequest> handler,
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
