namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Defines the configuration keys that the tests override.
/// </summary>
public static class GatewaySettingKeys
{
    public const string RedisConnectionString = "Redis:ConnectionString";
    public const string CorsAllowedOrigin = "Cors:AllowedOrigins:0";
    public const string CoreAddress = "ReverseProxy:Clusters:core:Destinations:main:Address";
    public const string CoreHealthAddress = "ReverseProxy:Clusters:core:Destinations:main:Health";
    public const string SpaAddress = "ReverseProxy:Clusters:spa:Destinations:main:Address";
    public const string SpaHealthCheckEnabled = "ReverseProxy:Clusters:spa:HealthCheck:Active:Enabled";
    public const string TelemetryAddress = "ReverseProxy:Clusters:telemetry:Destinations:main:Address";
    public const string DocumentStorageAddress = "ReverseProxy:Clusters:document-storage:Destinations:main:Address";
    public const string DocumentAddress = "ReverseProxy:Clusters:document:Destinations:main:Address";
    public const string DocumentHealthAddress = "ReverseProxy:Clusters:document:Destinations:main:Health";
    public const string InsightsAddress = "ReverseProxy:Clusters:insights:Destinations:main:Address";
    public const string InsightsHealthAddress = "ReverseProxy:Clusters:insights:Destinations:main:Health";
    public const string TelemetryHealthAddress = "ReverseProxy:Clusters:telemetry:Destinations:main:Health";
}
