using HealthyFac.Core.Application.WorkOrders.ApplySchedule;
using ApplicationApplyScheduleChangesRequest =
    HealthyFac.Core.Application.WorkOrders.ApplySchedule.ApplyScheduleChangesRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record ApplyScheduleChangesRequest
{
    public required IReadOnlyList<ScheduleChange> Changes { get; init; }
}

public static class ApplyScheduleChangesRequestExtensions
{
    extension(ApplyScheduleChangesRequest request)
    {
        public ApplicationApplyScheduleChangesRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Changes = request.Changes };
    }
}
