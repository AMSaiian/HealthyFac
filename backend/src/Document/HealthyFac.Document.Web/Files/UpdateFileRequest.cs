using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUpdateFileRequest = HealthyFac.Document.Application.Files.UpdateFileRequest;

namespace HealthyFac.Document.Web.Files;

public record UpdateFileRequest
{
    public required string FileName { get; init; }
    public string? Description { get; init; }
    public required uint RowVersion { get; init; }
}

public static class UpdateFileRequestExtensions
{
    extension(UpdateFileRequest request)
    {
        public ApplicationUpdateFileRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            Guid? machineId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = machineId,
                Id = FileRouteIds.Decrypt(encryptedId, idEncryptor),
                FileName = request.FileName,
                Description = request.Description,
                RowVersion = request.RowVersion,
            };
    }
}
