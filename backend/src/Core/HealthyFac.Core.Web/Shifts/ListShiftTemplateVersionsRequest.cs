using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationListShiftTemplateVersionsRequest =
    HealthyFac.Core.Application.Shifts.ListShiftTemplateVersionsRequest;

namespace HealthyFac.Core.Web.Shifts;

public record ListShiftTemplateVersionsRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }
}

public static class ListShiftTemplateVersionsRequestExtensions
{
    extension(ListShiftTemplateVersionsRequest request)
    {
        public ApplicationListShiftTemplateVersionsRequest ToApplicationRequest(
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                Scope = scope,
            };
    }
}
