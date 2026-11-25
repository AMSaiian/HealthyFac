namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Defines the validation messages of <see cref="DraftsProposed"/>.
/// </summary>
public static class DraftMessageErrors
{
    public static class Validation
    {
        public const string IdRequired = "Every ID is required.";
        public const string SourceRequired = "The source is required.";
        public const string QueuedAtRequired = "QueuedAt is required.";
        public const string IsMachineFileRequired = "IsMachineFile is required for an ingest.";
        public const string FieldsOfAnotherSource = "A field of the other source must be empty.";
        public const string MessageWithoutContent = "A message needs a machine, a metric, a status, or a phase.";
        public const string MachineDraftUnknown = "A MachineDraftId must name a create draft of the same message.";
        public const string UpdateWithoutContent = "An update draft needs a name, a serial number, or a description.";
        public const string DefinitionMachineInvalid = "A definition names exactly 1 machine.";
        public const string TargetMachineIdInvalid = "TargetMachineId must not be empty when it is set.";

        public const string MachineFileNeedsOneTarget =
            "A machine file holds at most 1 machine, and the machine needs a target.";

        public const string MachineWithoutContent = "A create draft needs a value or a definition that names it.";
        public const string MetricWithoutContent = "A metric needs at least one value.";
        public const string StatusWithoutContent = "A status needs at least one value.";
        public const string KeyInvalid = "The key does not match the key format.";
        public const string ValueKindInvalid = "The value kind is not allowed.";
        public const string SeverityInvalid = "The severity is not allowed.";
        public const string RangeInvalid = "Min must be below Max.";
        public const string WarningLowBelowAlarmLow = "WarningLow must not be below AlarmLow.";
        public const string WarningHighAboveAlarmHigh = "WarningHigh must not be above AlarmHigh.";
        public const string LimitsOnlyForNumeric = "Only a numeric metric has limits.";
        public const string BehaviorInvalid = "The behavior is not allowed.";
        public const string PriorityNegative = "Priority must not be negative.";
        public const string ConditionSourceInvalid = "A condition names exactly 1 source.";
        public const string LimitMetricInvalid = "A limit row names exactly 1 metric.";
        public const string OperatorInvalid = "The operator is not allowed.";
        public const string StatesInvalid = "States need at least 2 different values.";
        public const string DescriptionNotAllowed = "The description holds HTML that is not allowed.";
        public const string DescriptionTooMuchFormatting = "The description has too much formatting.";

        public static readonly string MachinesCount =
            $"A message holds at most {DraftMessageLimits.MaxMachines} machines.";

        public static readonly string MetricsCount =
            $"A machine holds at most {DraftMessageLimits.MaxMetrics} metrics in a message.";

        public static readonly string StatusesCount =
            $"A machine holds at most {DraftMessageLimits.MaxStatuses} statuses in a message.";

        public static readonly string DescriptionTooLong =
            $"The description has at most {DraftMessageLimits.DescriptionMaxLength} characters.";
    }
}
