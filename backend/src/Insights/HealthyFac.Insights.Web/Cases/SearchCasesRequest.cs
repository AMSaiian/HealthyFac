using HealthyFac.Insights.Application.Cases;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchCasesRequest = HealthyFac.Insights.Application.Cases.SearchCasesRequest;

namespace HealthyFac.Insights.Web.Cases;

public record SearchCasesRequest : GridRequest<CaseGridFilter, CaseSortColumn>;

public static class SearchCasesRequestExtensions
{
    extension(SearchCasesRequest request)
    {
        public ApplicationSearchCasesRequest ToApplicationRequest(Guid companyId) =>
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
