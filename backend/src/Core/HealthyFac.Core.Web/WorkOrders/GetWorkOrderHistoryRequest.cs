using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetWorkOrderHistoryRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderHistoryRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record GetWorkOrderHistoryRequest
{
    [FromRoute(Name = WorkOrderRoutes.IdParameter)]
    public required string Id { get; init; }
}

public static class GetWorkOrderHistoryRequestExtensions
{
    extension(GetWorkOrderHistoryRequest request)
    {
        public ApplicationGetWorkOrderHistoryRequest ToApplicationRequest(
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
