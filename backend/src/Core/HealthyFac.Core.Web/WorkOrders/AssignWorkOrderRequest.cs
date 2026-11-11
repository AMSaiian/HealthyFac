using HealthyFac.Shared.Application.IdEncryption;
using ApplicationAssignWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.AssignWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record AssignWorkOrderRequest
{
    public required Guid AssigneeId { get; init; }

    public required DateTime PlannedStartLocal { get; init; }

    public required uint RowVersion { get; init; }
}

public static class AssignWorkOrderRequestExtensions
{
    extension(AssignWorkOrderRequest request)
    {
        public ApplicationAssignWorkOrderRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = WorkOrderRouteIds.Decrypt(encryptedId, idEncryptor),
                AssigneeId = request.AssigneeId,
                PlannedStartLocal = request.PlannedStartLocal,
                RowVersion = request.RowVersion,
            };
    }
}
