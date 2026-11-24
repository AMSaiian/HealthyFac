namespace HealthyFac.Seeder.TelemetryHistory;

public static class TelemetryHistoryConstants
{
    public const string FileName = "telemetry.json";
    public const string StepPrefix = "step";
    public const string MarkerKey = "marker";
    public const string MaintenanceSuffix = ".maintenance";

    /// <summary>The names of the 4 lists as <c>telemetry.json</c> writes them.</summary>
    public const string DrainsList = "drains";

    public const string StepsList = "steps";
    public const string GapsList = "gaps";
    public const string OverridesList = "overrides";

    /// <summary>The drain time that a simulated machine gets. Each drain of the file sets its own time.</summary>
    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);
}
