using HealthyFac.Core.Application.Shifts;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUpdateShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.UpdateShiftTemplateVersionRequest;

namespace HealthyFac.Core.Web.Shifts;

public record UpdateShiftTemplateVersionRequest
{
    public required IReadOnlyList<ShiftTemplateDayInput> Days { get; init; }

    public required uint RowVersion { get; init; }
}

public static class UpdateShiftTemplateVersionRequestExtensions
{
    extension(UpdateShiftTemplateVersionRequest request)
    {
        public ApplicationUpdateShiftTemplateVersionRequest ToApplicationRequest(
            string encryptedEmployeeId,
            string encryptedVersionId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(encryptedEmployeeId, idEncryptor),
                VersionId = ShiftRouteIds.Decrypt(encryptedVersionId, idEncryptor),
                Days = request.Days,
                RowVersion = request.RowVersion,
            };
    }
}
