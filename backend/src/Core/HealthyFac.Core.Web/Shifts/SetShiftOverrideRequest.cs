using HealthyFac.Core.Application.Shifts;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationSetShiftOverrideRequest = HealthyFac.Core.Application.Shifts.SetShiftOverrideRequest;

namespace HealthyFac.Core.Web.Shifts;

public record SetShiftOverrideRequest
{
    public required IReadOnlyList<ShiftBlockInput> Blocks { get; init; }

    public uint? RowVersion { get; init; }
}

public static class SetShiftOverrideRequestExtensions
{
    extension(SetShiftOverrideRequest request)
    {
        public ApplicationSetShiftOverrideRequest ToApplicationRequest(
            string encryptedEmployeeId,
            DateOnly date,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(encryptedEmployeeId, idEncryptor),
                Date = date,
                Blocks = request.Blocks,
                RowVersion = request.RowVersion,
            };
    }
}
