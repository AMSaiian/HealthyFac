using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissDraftRequest = HealthyFac.Core.Application.Drafts.DismissDraftRequest;

namespace HealthyFac.Core.Web.Drafts;

public record DismissDraftRequest
{
    [FromRoute(Name = DraftRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class DismissDraftRequestExtensions
{
    extension(DismissDraftRequest request)
    {
        public ApplicationDismissDraftRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                Id = DraftRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
