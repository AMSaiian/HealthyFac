using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationChangeWorkOrderStatusRequest = HealthyFac.Core.Application.WorkOrders.ChangeWorkOrderStatusRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record ChangeWorkOrderStatusRequest
{
    public required WorkOrderStatus Status { get; init; }

    public required uint RowVersion { get; init; }
}

public static class ChangeWorkOrderStatusRequestExtensions
{
    extension(ChangeWorkOrderStatusRequest request)
    {
        public ApplicationChangeWorkOrderStatusRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Id = WorkOrderRouteIds.Decrypt(encryptedId, idEncryptor),
                Status = request.Status,
                RowVersion = request.RowVersion,
            };
    }
}
