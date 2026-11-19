namespace HealthyFac.Telemetry.Web.SignalEpisodes;

/// <summary>
/// Defines route constants for the signal read endpoints.
/// </summary>
public static class SignalRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string MetricDefinitionIdParameter = "metricDefinitionId";
    public const string FromParameter = "from";
    public const string ToParameter = "to";
    public const string BucketCountParameter = "bucketCount";
    public const string MachineRoute = "/machines/{" + MachineIdParameter + "}";
    public const string MetricHistory = "/metric-history";
    public const string SignalEpisodes = "/signal-episodes";
    public const string PhaseTimes = "/phase-times";
}
