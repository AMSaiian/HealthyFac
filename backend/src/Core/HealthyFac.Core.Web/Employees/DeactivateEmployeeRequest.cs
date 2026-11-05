using HealthyFac.Core.Web.Machines;
using ApplicationDeactivateEmployeeRequest =
    HealthyFac.Core.Application.Employees.DeactivateEmployee.DeactivateEmployeeRequest;

namespace HealthyFac.Core.Web.Employees;

public record DeactivateEmployeeRequest
{
    public required Guid EmployeeId { get; init; }
}

public static class DeactivateEmployeeRequestExtensions
{
    extension(DeactivateEmployeeRequest request)
    {
        public ApplicationDeactivateEmployeeRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = request.EmployeeId,
            };
    }
}
