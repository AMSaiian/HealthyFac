using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissDefinitionDraftRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.DismissDefinitionDraftRequest;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

public record DismissDefinitionDraftRequest
{
    [FromRoute(Name = DefinitionDraftRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DismissDefinitionDraftRequestExtensions
{
    extension(DismissDefinitionDraftRequest request)
    {
        public ApplicationDismissDefinitionDraftRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = DefinitionDraftRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
