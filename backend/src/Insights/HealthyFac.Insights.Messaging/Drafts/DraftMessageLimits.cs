namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Defines the limits of <see cref="DraftsProposed"/>. The field limits equal the limits of the targets, because
/// Messaging references no service layer.
/// </summary>
/// <remarks>
/// Sources: the machine limits are <c>MachineMessageLimits</c> and <c>MachineValidationConstants</c> (Core). The
/// metric and status limits are <c>MetricDefinitionValidationConstants</c>, <c>MetricDefinitionRules</c>, and
/// <c>StatusDefinitionValidationConstants</c> (Telemetry). The unit name limits are
/// <c>MetricUnitValidationConstants</c> (Telemetry).
/// </remarks>
public static class DraftMessageLimits
{
    public const int MinMachines = 1;
    public const int MaxMachines = 20;
    public const int MaxMetrics = 100;
    public const int MaxStatuses = 100;
    public const int MinStates = 2;

    public const int SourceLocationMaxLength = 200;
    public const int DescriptionMaxLength = 5000;

    public const string KeyPattern = "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)*$";

    public static class Machine
    {
        public const int NameMaxLength = 200;
        public const int SerialNumberMaxLength = 100;
    }

    public static class Metric
    {
        public const int KeyMaxLength = 100;
        public const int NameMaxLength = 100;
        public const int UnitNameMaxLength = 100;
        public const int StateMaxLength = 100;
    }

    public static class Status
    {
        public const int CodeMaxLength = 50;
    }

    public static class ValueKinds
    {
        public const string Numeric = "numeric";
        public const string Boolean = "boolean";
        public const string StateSet = "stateSet";

        public static readonly string[] All = [Numeric, Boolean, StateSet];
    }

    public static class Severities
    {
        public const string Info = "info";
        public const string Warning = "warning";
        public const string Error = "error";

        public static readonly string[] All = [Info, Warning, Error];
    }
}
