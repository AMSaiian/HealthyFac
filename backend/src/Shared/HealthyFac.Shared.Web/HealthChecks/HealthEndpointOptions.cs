namespace HealthyFac.Shared.Web.HealthChecks;

/// <summary>
/// Configures the internal port that serves the health endpoints. The service answers <c>/health</c>,
/// <c>/health/ready</c>, and <c>/hc</c> on this port only.
/// </summary>
public sealed class HealthEndpointOptions
{
    public const string SectionName = "HealthEndpoints";

    /// <summary>
    /// The port of the health endpoints. It must differ from the API port. The platform must not route it publicly.
    /// Add the same port to <c>ASPNETCORE_HTTP_PORTS</c> or to the URLs of the service, so Kestrel listens on it.
    /// </summary>
    public required int Port { get; init; }
}
