using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUpdateWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.UpdateWorkOrderRequest;

namespace HealthyFac.Core.Web.WorkOrders;

public record UpdateWorkOrderRequest
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public Guid? MachineId { get; init; }

    public required int EstimateMinutes { get; init; }

    public required IReadOnlyList<Guid> FileIds { get; init; }

    public required uint RowVersion { get; init; }
}

public static class UpdateWorkOrderRequestExtensions
{
    extension(UpdateWorkOrderRequest request)
    {
        public ApplicationUpdateWorkOrderRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Id = WorkOrderRouteIds.Decrypt(encryptedId, idEncryptor),
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Category = request.Category,
                MachineId = request.MachineId,
                EstimateMinutes = request.EstimateMinutes,
                FileIds = request.FileIds,
                RowVersion = request.RowVersion,
            };
    }
}
