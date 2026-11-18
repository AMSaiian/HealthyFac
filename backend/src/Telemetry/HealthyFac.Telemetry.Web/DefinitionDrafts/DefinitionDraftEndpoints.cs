using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissDefinitionDraftRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.DismissDefinitionDraftRequest;
using ApplicationDismissDefinitionDraftsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.DismissDefinitionDraftsRequest;
using ApplicationGetAttemptDefinitionDraftsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.GetAttemptDefinitionDraftsRequest;
using ApplicationGetPendingCountsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.GetPendingDefinitionDraftCountsRequest;
using ApplicationSearchDefinitionDraftsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts.SearchDefinitionDraftsRequest;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

/// <summary>
/// Configures endpoint routing for the metric and status drafts that the file ingest proposed.
/// </summary>
public static class DefinitionDraftEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all definition draft endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapDefinitionDraftEndpoints()
        {
            app.MapGroup(DefinitionDraftRoutes.MachineDraftsRoute)
                .MapPost(DefinitionDraftRoutes.Search, SearchMachineDraftsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Read);
            app.MapGroup(DefinitionDraftRoutes.AttemptDraftsRoute)
                .MapPost(DefinitionDraftRoutes.Search, SearchAttemptDraftsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Read);
            app.MapGroup(DefinitionDraftRoutes.AttemptDraftsRoute)
                .MapGet(DefinitionDraftRoutes.Root, GetAttemptDraftsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Read);
            var drafts = app.MapGroup(DefinitionDraftRoutes.DraftBaseRoute);
            drafts.MapGet(DefinitionDraftRoutes.PendingCounts, GetPendingCountsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Read);
            drafts.MapPost(DefinitionDraftRoutes.Dismiss, DismissAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Write);
            app.MapGroup(DefinitionDraftRoutes.MachineDraftsRoute)
                .MapPost(DefinitionDraftRoutes.DismissAll, DismissMachineDraftsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Write);
            app.MapGroup(DefinitionDraftRoutes.AttemptDraftsRoute)
                .MapPost(DefinitionDraftRoutes.DismissAll, DismissAttemptDraftsAsync)
                .RequireAuthorization(TelemetryPermissions.DefinitionDrafts.Write);

            return app;
        }
    }

    private static async Task<Ok<KeysetGridResponse<DefinitionDraftGridItem>>> SearchMachineDraftsAsync(
        [FromRoute(Name = DefinitionDraftRoutes.MachineIdParameter)] string machineId,
        SearchDefinitionDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchDefinitionDraftsRequest, KeysetGridResponse<DefinitionDraftGridItem>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(machineId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<KeysetGridResponse<DefinitionDraftGridItem>>> SearchAttemptDraftsAsync(
        [FromRoute(Name = DefinitionDraftRoutes.AttemptIdParameter)] string attemptId,
        SearchDefinitionDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchDefinitionDraftsRequest, KeysetGridResponse<DefinitionDraftGridItem>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationAttemptRequest(attemptId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<AttemptDefinitionDraftsResponse>> GetAttemptDraftsAsync(
        [FromRoute(Name = DefinitionDraftRoutes.AttemptIdParameter)] string attemptId,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetAttemptDefinitionDraftsRequest, AttemptDefinitionDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new ApplicationGetAttemptDefinitionDraftsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                AttemptId = DefinitionDraftRouteIds.Decrypt(attemptId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> DismissAsync(
        [AsParameters] DismissDefinitionDraftRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissDefinitionDraftRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<DismissDefinitionDraftsResponse>> DismissMachineDraftsAsync(
        [AsParameters] DismissMachineDefinitionDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissDefinitionDraftsRequest, DismissDefinitionDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<DismissDefinitionDraftsResponse>> DismissAttemptDraftsAsync(
        [AsParameters] DismissAttemptDefinitionDraftsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDismissDefinitionDraftsRequest, DismissDefinitionDraftsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<PendingDefinitionDraftCountsResponse>> GetPendingCountsAsync(
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetPendingCountsRequest, PendingDefinitionDraftCountsResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new ApplicationGetPendingCountsRequest { CompanyId = currentUser.GetCompanyId() },
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
