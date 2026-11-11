namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Defines the validation messages of <see cref="DraftsProposed"/>.
/// </summary>
public static class DraftMessageErrors
{
    public static class Validation
    {
        public const string IdRequired = "Every ID is required.";
        public const string QueuedAtRequired = "QueuedAt is required.";
        public const string TargetMachineIdInvalid = "TargetMachineId must not be empty when it is set.";

        public const string MachineFileNeedsOneTarget =
            "A machine file needs exactly 1 machine, and the machine needs a target.";

        public const string MachineWithoutContent = "A machine needs a value, a metric, or a status.";
        public const string MetricWithoutContent = "A metric needs at least one value.";
        public const string StatusWithoutContent = "A status needs at least one value.";
        public const string KeyInvalid = "The key does not match the key format.";
        public const string ValueKindInvalid = "The value kind is not allowed.";
        public const string SeverityInvalid = "The severity is not allowed.";
        public const string RangeInvalid = "Min must be below Max.";
        public const string StatesInvalid = "States need at least 2 different values.";
        public const string DescriptionNotAllowed = "The description holds HTML that is not allowed.";
        public const string DescriptionTooMuchFormatting = "The description has too much formatting.";

        public static readonly string MachinesCount =
            $"A message holds {DraftMessageLimits.MinMachines} to {DraftMessageLimits.MaxMachines} machines.";

        public static readonly string MetricsCount =
            $"A machine holds at most {DraftMessageLimits.MaxMetrics} metrics.";

        public static readonly string StatusesCount =
            $"A machine holds at most {DraftMessageLimits.MaxStatuses} statuses.";

        public static readonly string DescriptionTooLong =
            $"The description has at most {DraftMessageLimits.DescriptionMaxLength} characters.";
    }
}
