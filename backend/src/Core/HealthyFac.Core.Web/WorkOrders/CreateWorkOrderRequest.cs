using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using ApplicationCreateWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.CreateWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record CreateWorkOrderRequest
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public Guid? MachineId { get; init; }

    public required int EstimateMinutes { get; init; }

    public required IReadOnlyList<Guid> FileIds { get; init; }

    public Guid? AssigneeId { get; init; }

    public DateTime? PlannedStartLocal { get; init; }
}

public static class CreateWorkOrderRequestExtensions
{
    extension(CreateWorkOrderRequest request)
    {
        public ApplicationCreateWorkOrderRequest ToApplicationRequest(Guid companyId, AccessScope scope) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Category = request.Category,
                MachineId = request.MachineId,
                EstimateMinutes = request.EstimateMinutes,
                FileIds = request.FileIds,
                AssigneeId = request.AssigneeId,
                PlannedStartLocal = request.PlannedStartLocal,
            };
    }
}
