using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Shared.Application.DownstreamScopes;
using ContractFileInfo = HealthyFac.Document.Contracts.Files.FileInfo;

namespace HealthyFac.Insights.Infrastructure.Files;

/// <summary>
/// Reads file data from Document through its gRPC port. A user read exchanges the token of the user. A worker read
/// uses the service token with the actor headers, and enters the downstream scope of the call for the call only.
/// </summary>
/// <param name="clientFactory">Creates the named clients.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the worker call.</param>
public sealed class DocumentGrpcFileCatalog(
    GrpcClientFactory clientFactory,
    IDownstreamScopeHolder downstreamScopes
) : IFileCatalog
{
    private readonly GrpcClientFactory _clientFactory = clientFactory;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<IngestFile?> GetFileForUserAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient<DocumentFiles.DocumentFilesClient>(DocumentClientNames.User);

        try
        {
            var reply = await client.GetFileAsync(
                new GetFileRequest { FileId = fileId.ToString() },
                cancellationToken: cancellationToken);

            return ToIngestFile(reply);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IngestFileLink?> GetFileLinkForWorkerAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient<DocumentFiles.DocumentFilesClient>(DocumentClientNames.Worker);

        using var lease = _downstreamScopes.Enter(DocumentClientNames.WorkerDownstreamScope);

        try
        {
            var reply = await client.GetFileLinkAsync(
                new GetFileLinkRequest { FileId = fileId.ToString() },
                cancellationToken: cancellationToken);

            return new IngestFileLink
            {
                File = ToIngestFile(reply.File),
                Url = reply.Url,
                ExpiresAt = reply.ExpiresAt.ToDateTime(),
            };
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private static IngestFile ToIngestFile(ContractFileInfo info) =>
        new()
        {
            FileId = Guid.Parse(info.FileId),
            CompanyId = Guid.Parse(info.CompanyId),
            MachineId = info.HasMachineId ? Guid.Parse(info.MachineId) : null,
            MachineChangedAt = info.MachineChangedAt.ToDateTime(),
            Purpose = info.Purpose,
            FileName = info.FileName,
            Extension = info.Extension,
            SizeBytes = info.SizeBytes,
        };
}
