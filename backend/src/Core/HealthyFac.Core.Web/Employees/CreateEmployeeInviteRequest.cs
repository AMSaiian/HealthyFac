using HealthyFac.Core.Domain.Roles;

namespace HealthyFac.Core.Web.Employees;

public record CreateEmployeeInviteRequest
{
    public required string Email { get; init; }
    public required RoleCode RoleCode { get; init; }
    public required IReadOnlyList<Guid> DepartmentIds { get; init; }
}

public static class CreateEmployeeInviteRequestExtensions
{
    extension(CreateEmployeeInviteRequest request)
    {
        public Application.Employees.CreateEmployeeInvite.CreateEmployeeInviteRequest ToApplicationRequest(
            Guid companyId
        ) =>
            new()
            {
                CompanyId = companyId,
                Email = request.Email,
                RoleCode = request.RoleCode,
                DepartmentIds = request.DepartmentIds,
            };
    }
}
