using HealthyFac.Shared.Application.IdEncryption;
using ApplicationDismissWorkOrderDraftRequest =
    HealthyFac.Core.Application.WorkOrders.Drafts.DismissWorkOrderDraftRequest;

namespace HealthyFac.Core.Web.WorkOrders.Drafts;

public record DismissWorkOrderDraftRequest
{
    public uint? RowVersion { get; init; }
}

public static class DismissWorkOrderDraftRequestExtensions
{
    extension(DismissWorkOrderDraftRequest request)
    {
        public ApplicationDismissWorkOrderDraftRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = WorkOrderDraftRouteIds.Decrypt(encryptedId, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
