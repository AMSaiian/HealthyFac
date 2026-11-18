using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissDefinitionDraftsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.DismissDefinitionDraftsRequest;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

public record DismissMachineDefinitionDraftsRequest
{
    [FromRoute(Name = DefinitionDraftRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }
}

public record DismissAttemptDefinitionDraftsRequest
{
    [FromRoute(Name = DefinitionDraftRoutes.AttemptIdParameter)]
    public required string AttemptId { get; init; }
}

public static class DismissDefinitionDraftsRequestExtensions
{
    extension(DismissMachineDefinitionDraftsRequest request)
    {
        public ApplicationDismissDefinitionDraftsRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
            };
    }

    extension(DismissAttemptDefinitionDraftsRequest request)
    {
        public ApplicationDismissDefinitionDraftsRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                AttemptId = DefinitionDraftRouteIds.Decrypt(request.AttemptId, idEncryptor),
            };
    }
}
