using HealthyFac.Shared.Application.IdEncryption;
using ApplicationTieFileToMachineRequest = HealthyFac.Document.Application.Files.Machines.TieFileToMachineRequest;

namespace HealthyFac.Document.Web.Files;

public record TieFileToMachineRequest
{
    public required Guid MachineId { get; init; }
    public required uint RowVersion { get; init; }
}

public static class TieFileToMachineRequestExtensions
{
    extension(TieFileToMachineRequest request)
    {
        public ApplicationTieFileToMachineRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = FileRouteIds.Decrypt(encryptedId, idEncryptor),
                MachineId = request.MachineId,
                RowVersion = request.RowVersion,
            };
    }
}
