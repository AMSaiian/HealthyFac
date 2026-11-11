using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationUnassignWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.UnassignWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record UnassignWorkOrderRequest
{
    [FromRoute(Name = WorkOrderRoutes.IdParameter)]
    public required string Id { get; init; }

    [FromQuery(Name = WorkOrderRoutes.RowVersionParameter)]
    public required uint RowVersion { get; init; }
}

public static class UnassignWorkOrderRequestExtensions
{
    extension(UnassignWorkOrderRequest request)
    {
        public ApplicationUnassignWorkOrderRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                Id = WorkOrderRouteIds.Decrypt(request.Id, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
