using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record GetWorkOrderRequest
{
    [FromRoute(Name = WorkOrderRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetWorkOrderRequestExtensions
{
    extension(GetWorkOrderRequest request)
    {
        public ApplicationGetWorkOrderRequest ToApplicationRequest(
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Id = WorkOrderRouteIds.Decrypt(request.Id, idEncryptor),
            };
    }
}
