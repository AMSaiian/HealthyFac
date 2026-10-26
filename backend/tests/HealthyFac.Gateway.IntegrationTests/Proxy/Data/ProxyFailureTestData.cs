namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class ProxyFailureTestData
{
    public const string ClosedPortAddress = "http://127.0.0.1:1/";
    public const string MachinesRoute = "/api/core/v1/machines";
    public const string ProblemJsonMediaType = "application/problem+json";
    public const string SecretToken = "secret-token-value-123";
    public const string RouteIdKey = "RouteId";
    public const string CoreRouteId = "core";

    public static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan TimeoutResponseLimit = TimeSpan.FromSeconds(2.5);
    public static readonly TimeSpan UnhealthyWaitLimit = TimeSpan.FromSeconds(15);

    public static IReadOnlyDictionary<string, string> OneSecondActivityTimeout { get; } =
        new Dictionary<string, string>
        {
            ["ReverseProxy:Clusters:core:HttpRequest:ActivityTimeout"] = "00:00:01",
        };

    public static IReadOnlyDictionary<string, string> OneSecondHealthCheckInterval { get; } =
        new Dictionary<string, string>
        {
            ["ReverseProxy:Clusters:core:HealthCheck:Active:Interval"] = "00:00:01",
        };
}
