using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.DeleteWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record DeleteWorkOrderRequest
{
    [FromRoute(Name = WorkOrderRoutes.IdParameter)]
    public required string Id { get; init; }

    [FromQuery(Name = WorkOrderRoutes.RowVersionParameter)]
    public required uint RowVersion { get; init; }
}

public static class DeleteWorkOrderRequestExtensions
{
    extension(DeleteWorkOrderRequest request)
    {
        public ApplicationDeleteWorkOrderRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                Id = WorkOrderRouteIds.Decrypt(request.Id, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
