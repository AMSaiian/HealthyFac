using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationRevokeEmployeeInviteRequest =
    HealthyFac.Core.Application.Employees.RevokeEmployeeInviteRequest;

namespace HealthyFac.Core.Web.Employees;

public record RevokeEmployeeInviteRequest
{
    [FromRoute(Name = EmployeeInviteRoutes.IdParameter)]
    public required string Id { get; init; }

    [FromQuery(Name = EmployeeInviteRoutes.RowVersionParameter)]
    public required uint RowVersion { get; init; }
}

public static class RevokeEmployeeInviteRequestExtensions
{
    extension(RevokeEmployeeInviteRequest request)
    {
        public ApplicationRevokeEmployeeInviteRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = EmployeeInviteRouteIds.Decrypt(request.Id, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
