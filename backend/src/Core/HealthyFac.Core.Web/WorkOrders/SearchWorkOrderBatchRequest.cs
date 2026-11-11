using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using ApplicationSearchWorkOrderBatchRequest =
    HealthyFac.Core.Application.WorkOrders.SearchWorkOrders.SearchWorkOrderBatchRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record SearchWorkOrderBatchRequest
{
    public required IReadOnlyList<SearchWorkOrderBatchEntry> Searches { get; init; }
}

public static class SearchWorkOrderBatchRequestExtensions
{
    extension(SearchWorkOrderBatchRequest request)
    {
        public ApplicationSearchWorkOrderBatchRequest ToApplicationRequest(Guid companyId, AccessScope scope) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Searches = request.Searches,
            };
    }
}
