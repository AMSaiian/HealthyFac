using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationApplyMachineDraftRequest = HealthyFac.Core.Application.Drafts.ApplyMachineDraftRequest;
using ApplicationDismissDraftRequest = HealthyFac.Core.Application.Drafts.DismissDraftRequest;
using ApplicationDismissMachineDraftsRequest = HealthyFac.Core.Application.Drafts.DismissMachineDraftsRequest;
using ApplicationGetAttemptDraftsRequest = HealthyFac.Core.Application.Drafts.GetAttemptDraftsRequest;
using ApplicationGetMachineDraftRequest = HealthyFac.Core.Application.Drafts.GetMachineDraftRequest;
using ApplicationMarkDraftsAppliedRequest = HealthyFac.Core.Application.Drafts.MarkDraftsAppliedRequest;
using ApplicationSearchMachineDraftsRequest =
    HealthyFac.Core.Application.Drafts.SearchMachineDrafts.SearchMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

/// <summary>
/// Configures endpoint routing for the drafts that the file ingest proposed.
/// </summary>
public static class DraftEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all draft endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapDraftEndpoints()
        {
            var machineDrafts = app.MapGroup(DraftRoutes.MachineDraftBaseRoute);
            machineDrafts.MapGet(DraftRoutes.ById, GetMachineDraftAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            machineDrafts.MapPost(DraftRoutes.Apply, ApplyMachineDraftAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write)
                .RequireAuthorization(CorePermissions.Machines.Write);

            app.MapGroup(DraftRoutes.MachineDraftsOfMachineRoute)
                .MapPost(DraftRoutes.Search, SearchMachineDraftsAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            app.MapGroup(DraftRoutes.MachineDraftsOfMachineRoute)
                .MapPost(DraftRoutes.DismissAll, DismissMachineDraftsAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write);
            app.MapGroup(DraftRoutes.AttemptDraftsRoute)
                .MapPost(DraftRoutes.Search, SearchAttemptDraftsAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);
            app.MapGroup(DraftRoutes.AttemptDraftsRoute)
                .MapPost(DraftRoutes.DismissAll, DismissAttemptDraftsAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write);
            app.MapGroup(DraftRoutes.AttemptDraftsRoute)
                .MapGet(DraftRoutes.Root, GetAttemptDraftsAsync)
                .RequireAuthorization(CorePermissions.Drafts.Read);

            var drafts = app.MapGroup(DraftRoutes.DraftBaseRoute);
            drafts.MapPost(DraftRoutes.Dismiss, DismissAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write);
            drafts.MapPost(DraftRoutes.MarkApplied, MarkAppliedAsync)
                .RequireAuthorization(CorePermissions.Drafts.Write);

            return app;
        }
    }

    private static async Task<Ok<GridResponse<MachineDraftGridItem>>> SearchMachineDraftsAsync(
        [FromRoute(Name = DraftRoutes.MachineIdParameter)] string machineId,
        SearchMachineDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchMachineDraftsRequest, GridResponse<MachineDraftGridItem>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GridResponse<MachineDraftGridItem>>> SearchAttemptDraftsAsync(
        [FromRoute(Name = DraftRoutes.AttemptIdParameter)] string attemptId,
        SearchMachineDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchMachineDraftsRequest, GridResponse<MachineDraftGridItem>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationAttemptRequest(attemptId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<DismissMachineDraftsResponse>> DismissAttemptDraftsAsync(
        [AsParameters] DismissAttemptDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissMachineDraftsRequest, DismissMachineDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<DismissMachineDraftsResponse>> DismissMachineDraftsAsync(
        [AsParameters] DismissMachineDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissMachineDraftsRequest, DismissMachineDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<AttemptDraftsResponse>> GetAttemptDraftsAsync(
        [FromRoute(Name = DraftRoutes.AttemptIdParameter)] string attemptId,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetAttemptDraftsRequest, AttemptDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new ApplicationGetAttemptDraftsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                AttemptId = DraftRouteIds.Decrypt(attemptId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<MachineDraftResponse>> GetMachineDraftAsync(
        [FromRoute(Name = DraftRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMachineDraftRequest, MachineDraftResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new ApplicationGetMachineDraftRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = DraftRouteIds.Decrypt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> DismissAsync(
        [AsParameters] DismissDraftRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissDraftRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAppliedAsync(
        MarkDraftsAppliedRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationMarkDraftsAppliedRequest> handler,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(request.ToApplicationRequest(currentUser.GetCompanyId()), cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<ApplyMachineDraftResponse>> ApplyMachineDraftAsync(
        [FromRoute(Name = DraftRoutes.IdParameter)] string id,
        ApplyMachineDraftRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationApplyMachineDraftRequest, ApplyMachineDraftResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
