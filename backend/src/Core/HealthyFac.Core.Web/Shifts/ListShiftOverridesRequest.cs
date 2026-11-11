using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationListShiftOverridesRequest = HealthyFac.Core.Application.Shifts.ListShiftOverridesRequest;

namespace HealthyFac.Core.Web.Shifts;

public record ListShiftOverridesRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromQuery(Name = "from")]
    public required DateOnly From { get; init; }

    [FromQuery(Name = "to")]
    public required DateOnly To { get; init; }
}

public static class ListShiftOverridesRequestExtensions
{
    extension(ListShiftOverridesRequest request)
    {
        public ApplicationListShiftOverridesRequest ToApplicationRequest(
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                From = request.From,
                To = request.To,
                Scope = scope,
            };
    }
}
