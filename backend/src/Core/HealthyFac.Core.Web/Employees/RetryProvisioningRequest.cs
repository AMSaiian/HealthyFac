using ApplicationRetryProvisioningRequest =
    HealthyFac.Core.Application.Employees.RetryProvisioningRequest;

namespace HealthyFac.Core.Web.Employees;

public record RetryProvisioningRequest
{
    public required Guid EmployeeId { get; init; }
}

public static class RetryProvisioningRequestExtensions
{
    extension(RetryProvisioningRequest request)
    {
        public ApplicationRetryProvisioningRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = request.EmployeeId,
            };
    }
}
