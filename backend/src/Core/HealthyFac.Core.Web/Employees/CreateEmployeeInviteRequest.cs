using HealthyFac.Core.Domain.Roles;
using ApplicationCreateEmployeeInviteRequest =
    HealthyFac.Core.Application.Employees.CreateEmployeeInviteRequest;

namespace HealthyFac.Core.Web.Employees;

public record CreateEmployeeInviteRequest
{
    public required string Email { get; init; }
    public required RoleCode RoleCode { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public static class CreateEmployeeInviteRequestExtensions
{
    extension(CreateEmployeeInviteRequest request)
    {
        public ApplicationCreateEmployeeInviteRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                Email = request.Email,
                RoleCode = request.RoleCode,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };
    }
}
