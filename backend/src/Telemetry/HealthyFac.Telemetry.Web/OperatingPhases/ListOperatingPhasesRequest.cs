using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationListOperatingPhasesRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.ListOperatingPhasesRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

public record ListOperatingPhasesRequest
{
    [FromRoute(Name = OperatingPhaseRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }
}

public static class ListOperatingPhasesRequestExtensions
{
    extension(ListOperatingPhasesRequest request)
    {
        public ApplicationListOperatingPhasesRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
            };
    }
}
