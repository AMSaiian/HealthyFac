using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUntieFileFromMachineRequest =
    HealthyFac.Document.Application.Files.Machines.UntieFileFromMachineRequest;

namespace HealthyFac.Document.Web.Files;

public record UntieFileFromMachineRequest
{
    public required uint RowVersion { get; init; }
}

public static class UntieFileFromMachineRequestExtensions
{
    extension(UntieFileFromMachineRequest request)
    {
        public ApplicationUntieFileFromMachineRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = FileRouteIds.Decrypt(encryptedId, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
