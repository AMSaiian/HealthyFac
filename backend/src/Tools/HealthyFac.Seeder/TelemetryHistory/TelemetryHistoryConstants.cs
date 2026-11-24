namespace HealthyFac.Seeder.TelemetryHistory;

public static class TelemetryHistoryConstants
{
    public const string FileName = "telemetry.json";
    public const string StepPrefix = "step";
    public const string MarkerKey = "marker";
    public const string MaintenanceSuffix = ".maintenance";

    /// <summary>The drain time that a simulated machine gets. Each drain of the file sets its own time.</summary>
    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);
}
