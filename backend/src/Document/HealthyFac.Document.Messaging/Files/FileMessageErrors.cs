namespace HealthyFac.Document.Messaging.Files;

/// <summary>
/// Defines the validation messages of the file messages.
/// </summary>
public static class FileMessageErrors
{
    public static class Validation
    {
        public const string FileIdRequired = "FileId is required.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string MachineIdInvalid = "MachineId must not be empty when it is set.";
        public const string ChangedAtRequired = "ChangedAt is required.";
    }
}
