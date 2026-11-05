using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetMachineErrorCodeRequest =
    HealthyFac.Core.Application.MachineErrorCodes.GetMachineErrorCodeRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

public record GetMachineErrorCodeRequest
{
    [FromRoute(Name = MachineErrorCodeRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = MachineErrorCodeRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetMachineErrorCodeRequestExtensions
{
    extension(GetMachineErrorCodeRequest request)
    {
        public ApplicationGetMachineErrorCodeRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = MachineErrorCodeRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
