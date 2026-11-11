using ApplicationLookupEmployeesRequest = HealthyFac.Core.Application.Employees.LookupEmployeesRequest;

namespace HealthyFac.Core.Web.Employees;

public record LookupEmployeesRequest
{
    public string? Search { get; init; }
}

public static class LookupEmployeesRequestExtensions
{
    extension(LookupEmployeesRequest request)
    {
        public ApplicationLookupEmployeesRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Search = request.Search };
    }
}
