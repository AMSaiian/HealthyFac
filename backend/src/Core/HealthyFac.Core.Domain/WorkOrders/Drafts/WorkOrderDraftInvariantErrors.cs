using HealthyFac.Core.Domain.Drafts;

namespace HealthyFac.Core.Domain.WorkOrders.Drafts;

public static class WorkOrderDraftInvariantErrors
{
    public static InvalidOperationException NotPending(Guid draftId, DraftState state) =>
        new($"Work order draft '{draftId}' is {state.Value}, so it cannot change.");
}
