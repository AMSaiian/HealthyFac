namespace HealthyFac.Insights.Messaging.Ingest;

/// <summary>
/// Defines the validation messages of the ingest messages.
/// </summary>
public static class IngestMessageErrors
{
    public static class Validation
    {
        public const string IdRequired = "Every ID is required.";
        public const string StateInvalid = "State must be one of the ingest states.";
        public const string FailedCountNegative = "FailedCount must be 0 or more.";
        public const string ChangedAtRequired = "ChangedAt is required.";
    }
}
