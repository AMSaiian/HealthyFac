using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Application.Files.Lookup;
using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.Application.Files.Uploading;
using HealthyFac.Document.Web.Authorization;
using HealthyFac.Document.Web.MachineRegistrations;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteFileRequest = HealthyFac.Document.Application.Files.DeleteFileRequest;
using ApplicationTieFileToMachineRequest = HealthyFac.Document.Application.Files.Machines.TieFileToMachineRequest;
using ApplicationUntieFileFromMachineRequest =
    HealthyFac.Document.Application.Files.Machines.UntieFileFromMachineRequest;
using ApplicationUpdateFileRequest = HealthyFac.Document.Application.Files.UpdateFileRequest;
using ApplicationGetFileLinksRequest = HealthyFac.Document.Application.Files.Links.GetFileLinksRequest;
using ApplicationLookupFilesRequest = HealthyFac.Document.Application.Files.Lookup.LookupFilesRequest;
using ApplicationSearchFilesRequest = HealthyFac.Document.Application.Files.Searching.SearchFilesRequest;
using ApplicationUploadEmbeddedImageRequest =
    HealthyFac.Document.Application.Files.Uploading.UploadEmbeddedImageRequest;
using ApplicationUploadFileRequest = HealthyFac.Document.Application.Files.Uploading.UploadFileRequest;

namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Configures endpoint routing for the files of the company and of the machines.
/// </summary>
public static class FileEndpoints
{
    private const long RequestOverhead = FileValidationConstants.Size.MultipartOverheadBytes;

    private const long AttachmentRequestLimit = FileValidationConstants.Size.AttachmentMaxBytes + RequestOverhead;

    private const long EmbeddedRequestLimit = FileValidationConstants.Size.EmbeddedMaxBytes + RequestOverhead;

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all file endpoints to the application route builder.
        /// </summary>
        /// <remarks>
        /// The upload endpoints read a multipart form with a bearer token and no cookie, so they turn the antiforgery
        /// check off. Each upload runs under the <c>file-upload</c> concurrency limit and under the request size limit
        /// of its purpose.
        /// </remarks>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapFileEndpoints()
        {
            var companyGroup = app.MapGroup(FileRoutes.CompanyBaseRoute);
            companyGroup.MapPost(FileRoutes.Root, UploadCompanyFileAsync)
                .RequireAuthorization(DocumentPermissions.CompanyFiles.Write)
                .RequireRateLimiting(UploadLimits.PolicyName)
                .WithMetadata(new RequestSizeLimitAttribute(AttachmentRequestLimit))
                .DisableAntiforgery();
            companyGroup.MapPost(FileRoutes.Embedded, UploadEmbeddedImageAsync)
                .RequireAuthorization(DocumentPermissions.Files.Write)
                .RequireRateLimiting(UploadLimits.PolicyName)
                .WithMetadata(new RequestSizeLimitAttribute(EmbeddedRequestLimit))
                .DisableAntiforgery();

            companyGroup.MapPut(FileRoutes.ById, UpdateCompanyFileAsync)
                .RequireAuthorization(DocumentPermissions.CompanyFiles.Write);
            companyGroup.MapDelete(FileRoutes.ById, DeleteCompanyFileAsync)
                .RequireAuthorization(DocumentPermissions.CompanyFiles.Delete);
            companyGroup.MapPut(FileRoutes.MachineOfFile, TieFileToMachineAsync)
                .RequireAuthorization(DocumentPermissions.CompanyFiles.Write);
            companyGroup.MapDelete(FileRoutes.MachineOfFile, UntieFileFromMachineAsync)
                .RequireAuthorization(DocumentPermissions.CompanyFiles.Write);
            companyGroup.MapPost(FileRoutes.Links, GetFileLinksAsync)
                .RequireAuthorization(DocumentPermissions.Files.Read);
            companyGroup.MapPost(FileRoutes.Lookup, LookupFilesAsync)
                .RequireAuthorization(DocumentPermissions.Files.Read);
            companyGroup.MapPost(FileRoutes.Search, SearchCompanyFilesAsync)
                .RequireAuthorization(DocumentPermissions.Files.Read);

            var machineGroup = app.MapGroup(FileRoutes.MachineBaseRoute);
            machineGroup.MapPut(FileRoutes.ById, UpdateMachineFileAsync)
                .RequireAuthorization(DocumentPermissions.Files.Write);
            machineGroup.MapDelete(FileRoutes.ById, DeleteMachineFileAsync)
                .RequireAuthorization(DocumentPermissions.Files.Delete);
            machineGroup.MapPost(FileRoutes.Search, SearchMachineFilesAsync)
                .RequireAuthorization(DocumentPermissions.Files.Read);
            machineGroup.MapPost(FileRoutes.Root, UploadMachineFileAsync)
                .RequireAuthorization(DocumentPermissions.Files.Write)
                .RequireRateLimiting(UploadLimits.PolicyName)
                .WithMetadata(new RequestSizeLimitAttribute(AttachmentRequestLimit))
                .DisableAntiforgery();

            return app;
        }
    }

    private static async Task<NoContent> UpdateCompanyFileAsync(
        [FromRoute(Name = FileRoutes.IdParameter)] string id,
        UpdateFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateFileRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), machineId: null, idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteCompanyFileAsync(
        [AsParameters] DeleteFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteFileRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), machineId: null, idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> TieFileToMachineAsync(
        [FromRoute(Name = FileRoutes.IdParameter)] string id,
        TieFileToMachineRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationTieFileToMachineRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UntieFileFromMachineAsync(
        [FromRoute(Name = FileRoutes.IdParameter)] string id,
        [FromBody] UntieFileFromMachineRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUntieFileFromMachineRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UpdateMachineFileAsync(
        [FromRoute(Name = FileRoutes.MachineIdParameter)] string machineId,
        [FromRoute(Name = FileRoutes.IdParameter)]
        string id,
        UpdateFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateFileRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(
                id,
                currentUser.GetCompanyId(),
                MachineRouteIds.Decrypt(machineId, idEncryptor),
                idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteMachineFileAsync(
        [FromRoute(Name = FileRoutes.MachineIdParameter)] string machineId,
        [AsParameters] DeleteFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteFileRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                MachineRouteIds.Decrypt(machineId, idEncryptor),
                idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<GetFileLinksResponse>> GetFileLinksAsync(
        GetFileLinksRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetFileLinksRequest, GetFileLinksResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<LookupFilesResponse>> LookupFilesAsync(
        LookupFilesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationLookupFilesRequest, LookupFilesResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GridResponse<FileGridItem>>> SearchCompanyFilesAsync(
        SearchFilesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchFilesRequest, GridResponse<FileGridItem>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), machineId: null),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GridResponse<FileGridItem>>> SearchMachineFilesAsync(
        [FromRoute(Name = FileRoutes.MachineIdParameter)] string machineId,
        SearchFilesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchFilesRequest, GridResponse<FileGridItem>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), MachineRouteIds.Decrypt(machineId, idEncryptor)),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Created<UploadFileResponse>> UploadCompanyFileAsync(
        [FromForm] UploadFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUploadFileRequest, UploadFileResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), machineId: null),
            cancellationToken);

        return TypedResults.Created((string?)null, response);
    }

    private static async Task<Created<UploadFileResponse>> UploadEmbeddedImageAsync(
        [FromForm] UploadEmbeddedImageRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUploadEmbeddedImageRequest, UploadFileResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Created((string?)null, response);
    }

    private static async Task<Created<UploadFileResponse>> UploadMachineFileAsync(
        [FromRoute(Name = FileRoutes.MachineIdParameter)] string machineId,
        [FromForm] UploadFileRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUploadFileRequest, UploadFileResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), MachineRouteIds.Decrypt(machineId, idEncryptor)),
            cancellationToken);

        return TypedResults.Created((string?)null, response);
    }
}
