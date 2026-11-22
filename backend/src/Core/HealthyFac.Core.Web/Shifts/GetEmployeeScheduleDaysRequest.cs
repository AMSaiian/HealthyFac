using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetEmployeeScheduleDaysRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetEmployeeScheduleDaysRequest;

namespace HealthyFac.Core.Web.Shifts;

public record GetEmployeeScheduleDaysRequest
{
    [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)]
    public required string EmployeeId { get; init; }

    [FromQuery(Name = ShiftRoutes.FromParameter)]
    public required DateOnly From { get; init; }

    [FromQuery(Name = ShiftRoutes.ToParameter)]
    public required DateOnly To { get; init; }

    [FromQuery(Name = ShiftRoutes.ExcludeWorkOrderIdParameter)]
    public string? ExcludeWorkOrderId { get; init; }
}

public static class GetEmployeeScheduleDaysRequestExtensions
{
    extension(GetEmployeeScheduleDaysRequest request)
    {
        public ApplicationGetEmployeeScheduleDaysRequest ToApplicationRequest(
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
                ExcludeWorkOrderId = ShiftRouteIds.TryDecrypt(request.ExcludeWorkOrderId, idEncryptor),
                Scope = scope,
            };
    }
}
