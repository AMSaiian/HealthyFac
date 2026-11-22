using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetEmployeeFreeSpansRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetEmployeeFreeSpansRequest;

namespace HealthyFac.Core.Web.Shifts;

public record GetEmployeeFreeSpansRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromQuery(Name = ShiftRoutes.DateParameter)]
    public required DateOnly Date { get; init; }

    [FromQuery(Name = ShiftRoutes.ExcludeWorkOrderIdParameter)]
    public string? ExcludeWorkOrderId { get; init; }
}

public static class GetEmployeeFreeSpansRequestExtensions
{
    extension(GetEmployeeFreeSpansRequest request)
    {
        public ApplicationGetEmployeeFreeSpansRequest ToApplicationRequest(
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                EmployeeId = ShiftRouteIds.Decrypt(request.EmployeeId, idEncryptor),
                Date = request.Date,
                ExcludeWorkOrderId = ShiftRouteIds.TryDecrypt(request.ExcludeWorkOrderId, idEncryptor),
                Scope = scope,
            };
    }
}
