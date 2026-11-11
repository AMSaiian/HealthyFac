using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationDeleteShiftOverrideRequest = HealthyFac.Core.Application.Shifts.DeleteShiftOverrideRequest;

namespace HealthyFac.Core.Web.Shifts;

public record DeleteShiftOverrideRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromRoute(Name = ShiftRoutes.DateParameter)]
    public required DateOnly Date { get; init; }

    [FromQuery(Name = ShiftRoutes.RowVersionParameter)]
    public required uint RowVersion { get; init; }
}

public static class DeleteShiftOverrideRequestExtensions
{
    extension(DeleteShiftOverrideRequest request)
    {
        public ApplicationDeleteShiftOverrideRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                Date = request.Date,
                RowVersion = request.RowVersion,
            };
    }
}
