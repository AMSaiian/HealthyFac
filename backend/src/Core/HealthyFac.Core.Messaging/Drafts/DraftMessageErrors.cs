namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Defines the validation messages of the draft messages.
/// </summary>
public static class DraftMessageErrors
{
    public static class Validation
    {
        public const string DraftIdRequired = "DraftId is required.";
        public const string FileIdRequired = "FileId is required.";
        public const string MachineIdRequired = "MachineId is required.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string QueuedAtRequired = "QueuedAt is required.";
        public const string AttemptIdRequired = "AttemptId is required.";
        public const string ResolutionRequired = "Resolution is required.";
        public const string EntriesRequired = "At least 1 entry is required.";
        public const string EntryIdRequired = "EntryId is required.";

        public const string AcceptedEntryNeedsOneTarget =
            "An accepted entry needs exactly 1 target: a machine ID or a create machine draft.";

        public const string RejectedEntryHasNoTarget =
            "A rejected entry has no machine ID and no create machine draft.";
    }
}
