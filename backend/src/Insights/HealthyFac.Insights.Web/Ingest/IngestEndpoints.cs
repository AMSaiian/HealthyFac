using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Insights.Web.Ingest;

/// <summary>
/// Configures endpoint routing for the ingest attempts of files.
/// </summary>
public static class IngestEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all ingest endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapIngestEndpoints()
        {
            var fileGroup = app.MapGroup(IngestRoutes.FileBaseRoute);
            fileGroup.MapPost(IngestRoutes.Root, StartAsync)
                .RequireAuthorization(InsightsPermissions.Ingest.Write);
            fileGroup.MapGet(IngestRoutes.Root, GetFileAttemptsAsync)
                .RequireAuthorization(InsightsPermissions.Ingest.Read);

            var attemptGroup = app.MapGroup(IngestRoutes.AttemptBaseRoute);
            attemptGroup.MapGet(IngestRoutes.ById, GetAttemptAsync)
                .RequireAuthorization(InsightsPermissions.Ingest.Read);
            attemptGroup.MapPost(IngestRoutes.Cancel, CancelAsync)
                .RequireAuthorization(InsightsPermissions.Ingest.Write);

            return app;
        }
    }

    private static async Task<Ok<FileIngestAttemptsResponse>> GetFileAttemptsAsync(
        [FromRoute(Name = IngestRoutes.FileIdParameter)] string fileId,
        ICurrentUserProvider currentUser,
        IRequestHandler<GetFileIngestAttemptsRequest, FileIngestAttemptsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new GetFileIngestAttemptsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                FileId = IngestRouteIds.DecryptFile(fileId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IngestAttemptResponse>> GetAttemptAsync(
        [FromRoute(Name = IngestRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<GetIngestAttemptRequest, IngestAttemptResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new GetIngestAttemptRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = IngestRouteIds.DecryptAttempt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> CancelAsync(
        [FromRoute(Name = IngestRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<CancelIngestAttemptRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            new CancelIngestAttemptRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = IngestRouteIds.DecryptAttempt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> StartAsync(
        [FromRoute(Name = IngestRoutes.FileIdParameter)] string fileId,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<StartIngestAttemptRequest, StartIngestAttemptResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new StartIngestAttemptRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                FileId = IngestRouteIds.DecryptFile(fileId, idEncryptor),
            },
            cancellationToken);

        var path = (httpRequest.PathBase + httpRequest.Path).Value ?? string.Empty;
        var location = string.Format(
            IngestRoutes.AttemptLocationFormat,
            path[..path.IndexOf(IngestRoutes.FilesSegment, StringComparison.Ordinal)],
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }
}
