using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUpdateMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.UpdateMachineErrorCodeRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

public record UpdateMachineErrorCodeRequest
{
    public required string Code { get; init; }
    public string? Description { get; init; }
}

public static class UpdateMachineErrorCodeRequestExtensions
{
    extension(UpdateMachineErrorCodeRequest request)
    {
        public ApplicationUpdateMachineErrorCodeRequest ToApplicationRequest(
            string encryptedMachineId,
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Id = MachineErrorCodeRouteIds.Decrypt(encryptedId, idEncryptor),
                Code = request.Code,
                Description = request.Description,
            };
    }
}
