using HealthyFac.Core.Application.Authorization;
using ApplicationLookupWorkOrdersRequest = HealthyFac.Core.Application.WorkOrders.LookupWorkOrdersRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record LookupWorkOrdersRequest
{
    public string? Search { get; init; }
}

public static class LookupWorkOrdersRequestExtensions
{
    extension(LookupWorkOrdersRequest request)
    {
        public ApplicationLookupWorkOrdersRequest ToApplicationRequest(Guid companyId, AccessScope scope) =>
            new() { CompanyId = companyId, Scope = scope, Search = request.Search };
    }
}
