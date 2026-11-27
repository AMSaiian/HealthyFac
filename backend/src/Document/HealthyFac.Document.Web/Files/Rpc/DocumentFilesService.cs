using Grpc.Core;
using HealthyFac.Document.Application.Files.Ingest;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Application.Files.Lookup;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Document.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Authorization;
using ContractFileInfo = HealthyFac.Document.Contracts.Files.FileInfo;
using ApplicationLookupFilesRequest = HealthyFac.Document.Application.Files.Lookup.LookupFilesRequest;
using ContractSearchFilesRequest = HealthyFac.Document.Contracts.Files.SearchFilesRequest;
using ContractGetFileRequest = HealthyFac.Document.Contracts.Files.GetFileRequest;

namespace HealthyFac.Document.Web.Files.Rpc;

/// <summary>
/// Serves the file data to the services that read files (Insights) on the gRPC port. The caller sends the plain
/// GUID of the file as text. A user token or a service token with actor headers works; the actor company scopes
/// every read.
/// </summary>
/// <param name="fileHandler">Reads the file data.</param>
/// <param name="linkHandler">Reads the file data and signs a link.</param>
/// <param name="lookupHandler">Searches the files by name.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
[Authorize(Policy = DocumentPermissions.Files.Read)]
public sealed class DocumentFilesService(
    IRequestHandler<GetIngestFileRequest, IngestFileResponse> fileHandler,
    IRequestHandler<GetIngestFileLinkRequest, IngestFileLinkResponse> linkHandler,
    IRequestHandler<ApplicationLookupFilesRequest, LookupFilesResponse> lookupHandler,
    ICurrentUserProvider currentUser
) : DocumentFiles.DocumentFilesBase
{
    private readonly IRequestHandler<GetIngestFileRequest, IngestFileResponse> _fileHandler = fileHandler;
    private readonly IRequestHandler<GetIngestFileLinkRequest, IngestFileLinkResponse> _linkHandler = linkHandler;

    private readonly IRequestHandler<ApplicationLookupFilesRequest, LookupFilesResponse> _lookupHandler =
        lookupHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    public override async Task<ContractFileInfo> GetFile(ContractGetFileRequest request, ServerCallContext context)
    {
        var response = await _fileHandler.HandleAsync(
            new GetIngestFileRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                FileId = ParseFileId(request.FileId),
            },
            context.CancellationToken);

        return ToFileInfo(response);
    }

    public override async Task<FileLinkReply> GetFileLink(GetFileLinkRequest request, ServerCallContext context)
    {
        var response = await _linkHandler.HandleAsync(
            new GetIngestFileLinkRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                FileId = ParseFileId(request.FileId),
                Audience = FileLinkAudience.Internal,
            },
            context.CancellationToken);

        return new FileLinkReply
        {
            File = ToFileInfo(response.File),
            Url = response.Url,
            ExpiresAt = RpcTimestamps.ToTimestamp(response.ExpiresAt),
        };
    }

    public override async Task<SearchFilesReply> SearchFiles(
        ContractSearchFilesRequest request,
        ServerCallContext context
    )
    {
        var response = await _lookupHandler.HandleAsync(
            new ApplicationLookupFilesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Search = RpcInput.ReadText(request.Search) ?? string.Empty,
            },
            context.CancellationToken);

        var reply = new SearchFilesReply();
        reply.Files.AddRange(response.Files.Select(ToFoundFile));

        return reply;
    }

    private static FoundFile ToFoundFile(LookedUpFile file)
    {
        var found = new FoundFile
        {
            FileId = file.Id.ToString(),
            FileName = file.FileName,
            Extension = file.Extension,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
        };

        if (file.MachineId is { } machineId)
        {
            found.MachineId = machineId.ToString();
        }

        return found;
    }

    private static Guid ParseFileId(string fileId) =>
        Guid.TryParse(fileId, out var parsed) ? parsed : throw new NotFoundException(RpcErrors.FileNotFound);

    private static ContractFileInfo ToFileInfo(IngestFileResponse response)
    {
        var info = new ContractFileInfo
        {
            FileId = response.FileId.ToString(),
            CompanyId = response.CompanyId.ToString(),
            MachineChangedAt =
                RpcTimestamps.ToTimestamp(response.MachineChangedAt),
            Purpose = response.Purpose,
            FileName = response.FileName,
            Extension = response.Extension,
            SizeBytes = response.SizeBytes,
        };

        if (response.MachineId is { } machineId)
        {
            info.MachineId = machineId.ToString();
        }

        return info;
    }
}
