using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.DeleteMachineErrorCodeRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

public record DeleteMachineErrorCodeRequest
{
    [FromRoute(Name = MachineErrorCodeRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = MachineErrorCodeRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DeleteMachineErrorCodeRequestExtensions
{
    extension(DeleteMachineErrorCodeRequest request)
    {
        public ApplicationDeleteMachineErrorCodeRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = MachineErrorCodeRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
