using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Insights.Messaging.WorkOrders;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

public static class WorkOrderDraftTestData
{
    public const string Title = "Replace the filter";
    public const int ValidEstimate = 60;

    public const string ProposedPriority = "high";
    public const string ProposedCategory = "repair";
    public const int OffGridEstimate = 3;
    public const uint StaleRowVersion = uint.MaxValue;

    public static WorkOrderDraftProposed Proposal(
        Guid companyId,
        Guid machineId,
        Guid draftId,
        Guid? runId = null
    ) =>
        new()
        {
            Source = WorkOrderProposalSource.Case,
            DraftId = draftId,
            CompanyId = companyId,
            MachineId = machineId,
            CaseId = Guid.CreateVersion7(),
            RunId = runId ?? Guid.CreateVersion7(),
            Title = Title,
            Priority = ProposedPriority,
            Category = ProposedCategory,
            EstimateMinutes = ValidEstimate,
        };

    public static WorkOrderDraftProposed ChatProposal(Guid companyId, Guid? machineId, Guid draftId) =>
        new()
        {
            Source = WorkOrderProposalSource.Chat,
            DraftId = draftId,
            CompanyId = companyId,
            MachineId = machineId,
            ChatId = Guid.CreateVersion7(),
            ChatMessageId = Guid.CreateVersion7(),
            Title = Title,
            Priority = ProposedPriority,
            Category = ProposedCategory,
            EstimateMinutes = ValidEstimate,
        };

    public static ApplyWorkOrderDraftRequest Apply(
        Guid companyId,
        Guid draftId,
        int estimateMinutes = ValidEstimate,
        uint? rowVersion = null
    ) =>
        new()
        {
            CompanyId = companyId,
            Id = draftId,
            Scope = AccessScope.Company,
            RowVersion = rowVersion,
            Title = Title,
            Priority = WorkOrderPriority.High,
            Category = WorkOrderCategory.Repair,
            EstimateMinutes = estimateMinutes,
        };

    public static TheoryData<string, string, string> InvalidValues => new()
    {
        { "Unknown state", "state", "'bogus'" },
        { "Unknown priority", "priority", "'urgent'" },
        { "Unknown category", "category", "'cleaning'" },
        { "Estimate below the minimum", "estimate_minutes", "4" },
        { "Estimate above the maximum", "estimate_minutes", "721" },
        { "Blank title", "title", "'   '" },
    };
}
