using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetFileRequest = HealthyFac.Document.Application.Files.GetFileRequest;

namespace HealthyFac.Document.Web.Files;

public record GetFileRequest
{
    [FromRoute(Name = FileRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetFileRequestExtensions
{
    extension(GetFileRequest request)
    {
        public ApplicationGetFileRequest ToApplicationRequest(
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
