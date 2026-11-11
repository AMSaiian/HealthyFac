using HealthyFac.Core.Application.Employees.SearchEmployeeInvites;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchEmployeeInvitesRequest =
    HealthyFac.Core.Application.Employees.SearchEmployeeInvites.SearchEmployeeInvitesRequest;

namespace HealthyFac.Core.Web.Employees;

public record SearchEmployeeInvitesRequest : GridRequest<EmployeeInviteGridFilter, EmployeeInviteSortColumn>;

public static class SearchEmployeeInvitesRequestExtensions
{
    extension(SearchEmployeeInvitesRequest request)
    {
        public ApplicationSearchEmployeeInvitesRequest ToApplicationRequest(Guid companyId) =>
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
