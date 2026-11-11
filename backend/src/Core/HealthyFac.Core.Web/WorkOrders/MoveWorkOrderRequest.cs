using HealthyFac.Shared.Application.IdEncryption;
using ApplicationMoveWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.MoveWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record MoveWorkOrderRequest
{
    public Guid? AssigneeId { get; init; }

    public required DateTime PlannedStartLocal { get; init; }

    public required uint RowVersion { get; init; }
}

public static class MoveWorkOrderRequestExtensions
{
    extension(MoveWorkOrderRequest request)
    {
        public ApplicationMoveWorkOrderRequest ToApplicationRequest(
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
