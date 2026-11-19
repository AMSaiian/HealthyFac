using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.DeleteOperatingPhaseRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

public record DeleteOperatingPhaseRequest
{
    [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = OperatingPhaseRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DeleteOperatingPhaseRequestExtensions
{
    extension(DeleteOperatingPhaseRequest request)
    {
        public ApplicationDeleteOperatingPhaseRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = OperatingPhaseRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
