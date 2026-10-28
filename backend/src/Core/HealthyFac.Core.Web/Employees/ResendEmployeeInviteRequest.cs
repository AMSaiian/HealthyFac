namespace HealthyFac.Core.Web.Employees;

public record ResendEmployeeInviteRequest
{
    public required Guid InviteId { get; init; }
}

public static class ResendEmployeeInviteRequestExtensions
{
    extension(ResendEmployeeInviteRequest request)
    {
        public Application.Employees.ResendEmployeeInvite.ResendEmployeeInviteRequest ToApplicationRequest(
            Guid companyId
        ) =>
            new()
            {
                CompanyId = companyId,
                InviteId = request.InviteId,
            };
    }
}
