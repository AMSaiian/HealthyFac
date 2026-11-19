using System.Security.Claims;
using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationApplyWorkOrderDraftRequest = HealthyFac.Core.Application.WorkOrders.Drafts.ApplyWorkOrderDraftRequest;
using ApplicationDismissWorkOrderDraftRequest =
    HealthyFac.Core.Application.WorkOrders.Drafts.DismissWorkOrderDraftRequest;
using ApplicationGetWorkOrderDraftRequest = HealthyFac.Core.Application.WorkOrders.Drafts.GetWorkOrderDraftRequest;
using ApplicationSearchWorkOrderDraftsRequest =
    HealthyFac.Core.Application.WorkOrders.Drafts.SearchWorkOrderDraftsRequest;

namespace HealthyFac.Core.Web.WorkOrders.Drafts;

/// <summary>
/// Configures endpoint routing for the work order drafts that the planner of Insights proposed. Reading needs
/// <c>drafts:read</c>. Dismissing needs <c>drafts:write</c>. Applying needs <c>drafts:write</c> and
/// <c>work-orders:write</c>, and the work order scope of the token decides what the apply may set.
/// </summary>
public static class WorkOrderDraftEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all work order draft endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapWorkOrderDraftEndpoints()
        {
            var group = app.MapGroup(WorkOrderDraftRoutes.BaseRoute);
            group.MapPost(WorkOrderDraftRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            group.MapGet(WorkOrderDraftRoutes.ById, GetAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            group.MapPost(WorkOrderDraftRoutes.Apply, ApplyAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write)
                .RequireAuthorization(CorePermissions.WorkOrders.Write);
            group.MapPost(WorkOrderDraftRoutes.Dismiss, DismissAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write);

            return app;
        }
    }

    private static async Task<Ok<GridResponse<WorkOrderDraftResponse>>> SearchAsync(
        SearchWorkOrderDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchWorkOrderDraftsRequest, GridResponse<WorkOrderDraftResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<WorkOrderDraftResponse>> GetAsync(
        [FromRoute(Name = WorkOrderDraftRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetWorkOrderDraftRequest, WorkOrderDraftResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new ApplicationGetWorkOrderDraftRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = WorkOrderDraftRouteIds.Decrypt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<ApplyWorkOrderDraftResponse>> ApplyAsync(
        [FromRoute(Name = WorkOrderDraftRoutes.IdParameter)] string id,
        ApplyWorkOrderDraftRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationApplyWorkOrderDraftRequest, ApplyWorkOrderDraftResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                id,
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.WorkOrders.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> DismissAsync(
        [FromRoute(Name = WorkOrderDraftRoutes.IdParameter)] string id,
        DismissWorkOrderDraftRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissWorkOrderDraftRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
