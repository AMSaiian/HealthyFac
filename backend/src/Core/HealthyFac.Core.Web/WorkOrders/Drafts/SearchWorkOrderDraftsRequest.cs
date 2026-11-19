using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchWorkOrderDraftsRequest =
    HealthyFac.Core.Application.WorkOrders.Drafts.SearchWorkOrderDraftsRequest;

namespace HealthyFac.Core.Web.WorkOrders.Drafts;

public record SearchWorkOrderDraftsRequest : GridRequest<WorkOrderDraftGridFilter, WorkOrderDraftSortColumn>;

public static class SearchWorkOrderDraftsRequestExtensions
{
    extension(SearchWorkOrderDraftsRequest request)
    {
        public ApplicationSearchWorkOrderDraftsRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
