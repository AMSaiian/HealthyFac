using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetEmployeeDayScheduleRequest = HealthyFac.Core.Application.Shifts.GetEmployeeDayScheduleRequest;

namespace HealthyFac.Core.Web.Shifts;

public record GetEmployeeDayScheduleRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromQuery(Name = ShiftRoutes.DateParameter)]
    public required DateOnly Date { get; init; }
}

public static class GetEmployeeDayScheduleRequestExtensions
{
    extension(GetEmployeeDayScheduleRequest request)
    {
        public ApplicationGetEmployeeDayScheduleRequest ToApplicationRequest(
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                Date = request.Date,
                Scope = scope,
            };
    }
}
