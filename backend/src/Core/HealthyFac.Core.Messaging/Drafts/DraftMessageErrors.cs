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
        public const string FileAndQueuedAtTogether = "FileId and QueuedAt are set together or both missing.";
        public const string FileIdEmpty = "FileId must not be empty when it is set.";
        public const string QueuedAtEmpty = "QueuedAt must not be empty when it is set.";
        public const string AttemptIdRequired = "AttemptId is required for an ingest proposal.";
        public const string AttemptIdOnlyForIngest = "AttemptId is allowed only for an ingest proposal.";
        public const string SourceRequired = "Source is required.";
        public const string ResolutionRequired = "Resolution is required.";
        public const string EntriesRequired = "At least 1 entry is required.";
        public const string EntryIdRequired = "EntryId is required.";

        public const string AcceptedEntryNeedsOneTarget =
            "An accepted entry needs exactly 1 target: a machine ID or a create machine draft.";

        public const string RejectedEntryHasNoTarget =
            "A rejected entry has no machine ID and no create machine draft.";
    }
}
