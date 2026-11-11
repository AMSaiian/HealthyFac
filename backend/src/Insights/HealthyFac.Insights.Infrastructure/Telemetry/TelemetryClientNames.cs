namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Defines the names that tie the Telemetry HTTP client to the identity setup.
/// </summary>
public static class TelemetryClientNames
{
    /// <summary>The name of the HTTP client that calls Telemetry with the service token and actor headers.</summary>
    public const string Client = "telemetry";

    /// <summary>The downstream scope of every Telemetry call. The token handler adds the service name.</summary>
    public const string DownstreamScope = "telemetry";
}
