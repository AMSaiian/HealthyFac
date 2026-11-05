using HealthyFac.Core.Web.Employees;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetMachineRequest = HealthyFac.Core.Application.Machines.GetMachineRequest;

namespace HealthyFac.Core.Web.Machines;

public record GetMachineRequest
{
    [FromRoute(Name = MachineRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetMachineRequestExtensions
{
    extension(GetMachineRequest request)
    {
        public ApplicationGetMachineRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                Id = MachineRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
