using Microsoft.AspNetCore.Mvc;
using ApplicationGetCompanyDayScheduleRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetCompanyDayScheduleRequest;

namespace HealthyFac.Core.Web.Shifts;

public record GetCompanyDayScheduleRequest
{
    [FromQuery(Name = ShiftRoutes.DateParameter)]
    public required DateOnly Date { get; init; }
}

public static class GetCompanyDayScheduleRequestExtensions
{
    extension(GetCompanyDayScheduleRequest request)
    {
        public ApplicationGetCompanyDayScheduleRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Date = request.Date };
    }
}
