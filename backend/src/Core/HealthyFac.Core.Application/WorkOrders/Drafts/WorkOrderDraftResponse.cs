using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

/// <summary>
/// The work order draft as the API shows it. The case and the run are the IDs of Insights.
/// </summary>
public record WorkOrderDraftResponse
{
    public required Guid Id { get; init; }

    public required Guid MachineId { get; init; }

    public string? MachineName { get; init; }

    public required bool IsMachineDeleted { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid RunId { get; init; }

    public required DraftSource Source { get; init; }

    public Guid? ChatId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public required int EstimateMinutes { get; init; }

    public required DraftState State { get; init; }

    public Guid? AppliedWorkOrderId { get; init; }

    public required DateTime ProposedAt { get; init; }

    public required uint RowVersion { get; init; }

    /// <summary>
    /// Builds the query projection. The machine name comes from the machine, also a deleted one, so an old draft keeps
    /// the name.
    /// </summary>
    /// <param name="dbContext">Reads the machine names.</param>
    /// <returns>The projection from a draft to its response.</returns>
    public static System.Linq.Expressions.Expression<Func<WorkOrderDraft, WorkOrderDraftResponse>> ProjectionFor(
        ICoreDbContext dbContext
    )
    {
        string[] deletedFilters = [SoftDeleteConstants.QueryFilterName];

        return draft => new WorkOrderDraftResponse
        {
            Id = draft.Id,
            MachineId = draft.MachineId,
            IsMachineDeleted = dbContext.Machines
                .IgnoreQueryFilters(deletedFilters)
                .Where(machine => machine.Id == draft.MachineId)
                .Select(machine => machine.DeletedAt != null)
                .FirstOrDefault(),
            MachineName = dbContext.Machines
                .IgnoreQueryFilters(deletedFilters)
                .Where(machine => machine.Id == draft.MachineId)
                .Select(machine => machine.Name)
                .FirstOrDefault(),
            CaseId = draft.Proposal.CaseId!.Value,
            RunId = draft.Proposal.RunId!.Value,
            Source = draft.Proposal.Source,
            ChatId = draft.Proposal.ChatId,
            Title = draft.Title,
            Description = draft.Description,
            Priority = draft.Priority,
            Category = draft.Category,
            EstimateMinutes = draft.EstimateMinutes,
            State = draft.State,
            AppliedWorkOrderId = draft.AppliedWorkOrderId,
            ProposedAt = draft.CreatedAt,
            RowVersion = draft.RowVersion,
        };
    }
}
