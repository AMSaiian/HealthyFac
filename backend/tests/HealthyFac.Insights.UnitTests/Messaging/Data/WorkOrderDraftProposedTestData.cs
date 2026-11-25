using HealthyFac.Insights.Messaging.WorkOrders;

namespace HealthyFac.Insights.UnitTests.Messaging.Data;

public static class WorkOrderDraftProposedTestData
{
    public static TheoryData<WorkOrderDraftProposed, string> InvalidMessages =>
        new()
        {
            { Case() with { Source = null! }, WorkOrderDraftMessageErrors.Validation.SourceRequired },
            { Case() with { MachineId = null }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            { Case() with { CaseId = null }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            { Case() with { CaseId = Guid.Empty }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            { Case() with { RunId = null }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            {
                Case() with { ChatId = Guid.CreateVersion7() },
                WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Case() with { ChatMessageId = Guid.CreateVersion7() },
                WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            { Chat() with { ChatId = null }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            { Chat() with { ChatMessageId = null }, WorkOrderDraftMessageErrors.Validation.IdRequired },
            {
                Chat() with { CaseId = Guid.CreateVersion7() },
                WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Chat() with { RunId = Guid.CreateVersion7() },
                WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            { Chat() with { MachineId = Guid.Empty }, WorkOrderDraftMessageErrors.Validation.IdRequired },
        };

    public static TheoryData<WorkOrderDraftProposed> ValidMessages =>
        new()
        {
            Case(),
            Chat(),
            Chat() with { MachineId = Guid.CreateVersion7() },
        };

    public static WorkOrderDraftProposed Case() =>
        new()
        {
            Source = WorkOrderProposalSource.Case,
            DraftId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            MachineId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            RunId = Guid.CreateVersion7(),
            Title = "Replace the filter",
            Priority = WorkOrderDraftMessageLimits.Priorities.High,
            Category = WorkOrderDraftMessageLimits.Categories.Repair,
            EstimateMinutes = 60,
        };

    public static WorkOrderDraftProposed Chat() =>
        new()
        {
            Source = WorkOrderProposalSource.Chat,
            DraftId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            ChatId = Guid.CreateVersion7(),
            ChatMessageId = Guid.CreateVersion7(),
            Title = "Replace the filter",
            Priority = WorkOrderDraftMessageLimits.Priorities.High,
            Category = WorkOrderDraftMessageLimits.Categories.Repair,
            EstimateMinutes = 60,
        };
}
