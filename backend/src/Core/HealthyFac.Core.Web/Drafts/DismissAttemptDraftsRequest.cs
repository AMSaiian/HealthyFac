using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissMachineDraftsRequest = HealthyFac.Core.Application.Drafts.DismissMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

public record DismissAttemptDraftsRequest
{
    [FromRoute(Name = DraftRoutes.AttemptIdParameter)]
    public required string AttemptId { get; init; }
}

public static class DismissAttemptDraftsRequestExtensions
{
    extension(DismissAttemptDraftsRequest request)
    {
        public ApplicationDismissMachineDraftsRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                AttemptId = DraftRouteIds.Decrypt(request.AttemptId, idEncryptor),
            };
    }
}
