namespace HealthyFac.Core.Application.WorkOrders.Drafts;

public static class WorkOrderDraftErrors
{
    public const string DraftNotFound = "The work order draft was not found.";
    public const string DraftNotPending = "The work order draft is not pending.";
    public const string DraftChanged = "The work order draft changed. Reload it and try again.";
    public const string MachineNotFound = "The machine of the draft was not found.";

    public static class Validation
    {
        public const string DraftIdRequired = "DraftId is required.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string MachineIdInvalid = "MachineId must not be empty.";
        public const string StateUnknown = "The state is not a draft state.";
    }
}
