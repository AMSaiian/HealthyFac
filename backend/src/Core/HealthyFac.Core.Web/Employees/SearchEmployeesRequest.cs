using HealthyFac.Core.Application.Employees.SearchEmployees;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchEmployeesRequest = HealthyFac.Core.Application.Employees.SearchEmployees.SearchEmployeesRequest;

namespace HealthyFac.Core.Web.Employees;

public record SearchEmployeesRequest : GridRequest<EmployeeGridFilter, EmployeeSortColumn>;

public static class SearchEmployeesRequestExtensions
{
    extension(SearchEmployeesRequest request)
    {
        public ApplicationSearchEmployeesRequest ToApplicationRequest(Guid companyId) =>
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
