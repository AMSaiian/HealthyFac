namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Defines the validation messages of the signal messages.
/// </summary>
public static class SignalMessageErrors
{
    public static class Validation
    {
        public const string EpisodeIdRequired = "EpisodeId is required.";
        public const string MachineIdRequired = "MachineId is required.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string RuleRequired = "Rule is required.";
        public const string SeverityRequired = "Severity is required.";
        public const string StateRequired = "State is required.";
        public const string OpenedAtRequired = "OpenedAt is required.";
        public const string ChangedAtRequired = "ChangedAt is required.";
        public const string SequenceTooLow = "Sequence must be at least 1.";
        public const string ResolvedAtWithOpenState = "ResolvedAt is allowed only with the state resolved.";
        public const string ResolvedAtRequired = "ResolvedAt is required with the state resolved.";
    }
}
