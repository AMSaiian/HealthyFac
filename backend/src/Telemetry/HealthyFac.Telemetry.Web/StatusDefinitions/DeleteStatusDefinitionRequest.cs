using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.DeleteStatusDefinitionRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

public record DeleteStatusDefinitionRequest
{
    [FromRoute(Name = StatusDefinitionRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromRoute(Name = StatusDefinitionRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DeleteStatusDefinitionRequestExtensions
{
    extension(DeleteStatusDefinitionRequest request)
    {
        public ApplicationDeleteStatusDefinitionRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Id = StatusDefinitionRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
