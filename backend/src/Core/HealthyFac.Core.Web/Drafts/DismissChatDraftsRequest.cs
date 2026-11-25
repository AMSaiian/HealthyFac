using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissMachineDraftsRequest = HealthyFac.Core.Application.Drafts.DismissMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

public record DismissChatDraftsRequest
{
    [FromRoute(Name = DraftRoutes.ChatIdParameter)]
    public required string ChatId { get; init; }
}

public static class DismissChatDraftsRequestExtensions
{
    extension(DismissChatDraftsRequest request)
    {
        public ApplicationDismissMachineDraftsRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                ChatId = DraftRouteIds.Decrypt(request.ChatId, idEncryptor),
            };
    }
}
