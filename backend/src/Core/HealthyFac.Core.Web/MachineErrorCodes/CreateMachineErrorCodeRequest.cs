using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationCreateMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.CreateMachineErrorCodeRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

public record CreateMachineErrorCodeRequest
{
    public required string Code { get; init; }
    public string? Description { get; init; }
}

public static class CreateMachineErrorCodeRequestExtensions
{
    extension(CreateMachineErrorCodeRequest request)
    {
        public ApplicationCreateMachineErrorCodeRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Code = request.Code,
                Description = request.Description,
            };
    }
}
