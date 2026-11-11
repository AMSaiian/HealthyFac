using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.DeleteShiftTemplateVersionRequest;

namespace HealthyFac.Core.Web.Shifts;

public record DeleteShiftTemplateVersionRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromRoute(Name = ShiftRoutes.VersionIdParameter)]
    public required string VersionId { get; init; }

    [FromQuery(Name = ShiftRoutes.RowVersionParameter)]
    public required uint RowVersion { get; init; }
}

public static class DeleteShiftTemplateVersionRequestExtensions
{
    extension(DeleteShiftTemplateVersionRequest request)
    {
        public ApplicationDeleteShiftTemplateVersionRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                VersionId = ShiftRouteIds.Decrypt(request.VersionId, idEncryptor),
                RowVersion = request.RowVersion,
            };
    }
}
