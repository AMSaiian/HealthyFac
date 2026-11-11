using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDismissMachineDraftsRequest = HealthyFac.Core.Application.Drafts.DismissMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

public record DismissMachineDraftsRequest
{
    [FromRoute(Name = DraftRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }
}

public static class DismissMachineDraftsRequestExtensions
{
    extension(DismissMachineDraftsRequest request)
    {
        public ApplicationDismissMachineDraftsRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = DraftRouteIds.Decrypt(request.MachineId, idEncryptor),
            };
    }
}
