using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.GetOperatingPhaseRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

public record GetOperatingPhaseRequest
{
    [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = OperatingPhaseRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetOperatingPhaseRequestExtensions
{
    extension(GetOperatingPhaseRequest request)
    {
        public ApplicationGetOperatingPhaseRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = OperatingPhaseRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
