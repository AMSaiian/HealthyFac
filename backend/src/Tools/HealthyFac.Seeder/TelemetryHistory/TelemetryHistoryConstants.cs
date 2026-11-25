namespace HealthyFac.Seeder.TelemetryHistory;

public static class TelemetryHistoryConstants
{
    public const string FileName = "telemetry.json";
    public const string StepPrefix = "step";
    public const string MarkerKey = "marker";
    public const string MaintenanceSuffix = ".maintenance";
    public const string WorkOrderSuffix = "-work-order";
    public const string LinkSuffix = ".link";

    /// <summary>The key of the supervisor who creates and assigns the work orders of the drains.</summary>
    public const string WorkOrderCreator = "nadiia";

    /// <summary>The minutes from the creation to the assignment of a drain work order.</summary>
    public const int AssignDelayMinutes = 5;

    /// <summary>The minutes from the link to the start of the maintenance.</summary>
    public const int LinkLeadMinutes = 1;

    /// <summary>The days after the assignment in which the job looks for a work block of the assignee.</summary>
    public const int PlannedStartSearchDays = 14;

    /// <summary>The names of the 4 lists as <c>telemetry.json</c> writes them.</summary>
    public const string DrainsList = "drains";

    public const string StepsList = "steps";
    public const string GapsList = "gaps";
    public const string OverridesList = "overrides";

    /// <summary>How long before the window start the first state row of a simulated machine lies.</summary>
    public static readonly TimeSpan InitialStateLead = TimeSpan.FromDays(days: 30);

    /// <summary>The drain time that a simulated machine gets. Each drain of the file sets its own time.</summary>
    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);
}
