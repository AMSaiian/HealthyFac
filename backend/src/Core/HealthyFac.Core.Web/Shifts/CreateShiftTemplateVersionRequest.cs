using HealthyFac.Core.Application.Shifts;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationCreateShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.CreateShiftTemplateVersionRequest;

namespace HealthyFac.Core.Web.Shifts;

public record CreateShiftTemplateVersionRequest
{
    public required DateOnly EffectiveFrom { get; init; }

    public required IReadOnlyList<ShiftTemplateDayInput> Days { get; init; }
}

public static class CreateShiftTemplateVersionRequestExtensions
{
    extension(CreateShiftTemplateVersionRequest request)
    {
        public ApplicationCreateShiftTemplateVersionRequest ToApplicationRequest(
            string encryptedEmployeeId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(encryptedEmployeeId, idEncryptor),
                EffectiveFrom = request.EffectiveFrom,
                Days = request.Days,
            };
    }
}
