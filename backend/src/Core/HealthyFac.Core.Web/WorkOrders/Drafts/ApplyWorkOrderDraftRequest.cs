using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationApplyWorkOrderDraftRequest = HealthyFac.Core.Application.WorkOrders.Drafts.ApplyWorkOrderDraftRequest;

namespace HealthyFac.Core.Web.WorkOrders.Drafts;

public record ApplyWorkOrderDraftRequest
{
    public uint? RowVersion { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public required int EstimateMinutes { get; init; }

    public Guid? AssigneeId { get; init; }

    public DateTime? PlannedStartLocal { get; init; }
}

public static class ApplyWorkOrderDraftRequestExtensions
{
    extension(ApplyWorkOrderDraftRequest request)
    {
        public ApplicationApplyWorkOrderDraftRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            AccessScope scope,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Scope = scope,
                Id = WorkOrderDraftRouteIds.Decrypt(encryptedId, idEncryptor),
                RowVersion = request.RowVersion,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Category = request.Category,
                EstimateMinutes = request.EstimateMinutes,
                AssigneeId = request.AssigneeId,
                PlannedStartLocal = request.PlannedStartLocal,
            };
    }
}
