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
    }
}
