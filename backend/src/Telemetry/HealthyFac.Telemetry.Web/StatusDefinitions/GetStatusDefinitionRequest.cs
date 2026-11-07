using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.GetStatusDefinitionRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

public record GetStatusDefinitionRequest
{
    [FromRoute(Name = StatusDefinitionRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = StatusDefinitionRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetStatusDefinitionRequestExtensions
{
    extension(GetStatusDefinitionRequest request)
    {
        public ApplicationGetStatusDefinitionRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = StatusDefinitionRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
