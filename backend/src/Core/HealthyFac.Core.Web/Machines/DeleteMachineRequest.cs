using HealthyFac.Core.Web.Employees;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteMachineRequest = HealthyFac.Core.Application.Machines.DeleteMachineRequest;

namespace HealthyFac.Core.Web.Machines;

public record DeleteMachineRequest
{
    [FromRoute(Name = MachineRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DeleteMachineRequestExtensions
{
    extension(DeleteMachineRequest request)
    {
        public ApplicationDeleteMachineRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                Id = MachineRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
