namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Defines activity source names and tag keys for use case tracing.
/// </summary>
public static class ObservabilityConstants
{
    public static class Sources
    {
        public const string UseCases = "HealthyFac.UseCases";
        public const string IdEncryption = "HealthyFac.IdEncryption";
    }

    public static class Spans
    {
        public const string Validation = "Validation";
        public const string Encrypt = "IdEncryption.Encrypt";
        public const string Decrypt = "IdEncryption.Decrypt";
    }

    public static class Tags
    {
        public const string UseCase = "healthyfac.use_case";
        public const string Outcome = "healthyfac.outcome";
        public const string ValidatorCount = "healthyfac.validator_count";
        public const string RejectionCount = "healthyfac.rejection_count";
    }

    public static class Outcomes
    {
        public const string Success = "success";
        public const string Rejected = "rejected";
        public const string Failure = "failure";
    }
}
