using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchWorkOrdersRequest =
    HealthyFac.Core.Application.WorkOrders.SearchWorkOrders.SearchWorkOrdersRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record SearchWorkOrdersRequest : GridRequest<WorkOrderGridFilter, WorkOrderSortColumn>;

public static class SearchWorkOrdersRequestExtensions
{
    extension(SearchWorkOrdersRequest request)
    {
        public ApplicationSearchWorkOrdersRequest ToApplicationRequest(Guid companyId, AccessScope scope) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
