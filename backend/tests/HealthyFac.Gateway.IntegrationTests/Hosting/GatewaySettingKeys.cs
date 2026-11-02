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
}
