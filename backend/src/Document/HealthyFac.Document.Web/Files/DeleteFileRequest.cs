using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteFileRequest = HealthyFac.Document.Application.Files.DeleteFileRequest;

namespace HealthyFac.Document.Web.Files;

public record DeleteFileRequest
{
    [FromRoute(Name = FileRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DeleteFileRequestExtensions
{
    extension(DeleteFileRequest request)
    {
        public ApplicationDeleteFileRequest ToApplicationRequest(
            Guid companyId,
            Guid? machineId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = machineId,
                Id = FileRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
