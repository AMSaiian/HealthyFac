namespace HealthyFac.Gateway.IntegrationTests.Authentication.Data;

public static class FallbackPolicyTestData
{
    public const string UnprotectedRoute = "/api/unprotected/v1/x";
    public const string UnknownRoute = "/api/other/v1/x";

    private const string RoutePrefix = "ReverseProxy:Routes:unprotected";

    public static IReadOnlyDictionary<string, string> UnprotectedRouteSettings { get; } =
        new Dictionary<string, string>
        {
            [$"{RoutePrefix}:ClusterId"] = "core",
            [$"{RoutePrefix}:RateLimiterPolicy"] = "default-limit",
            [$"{RoutePrefix}:Match:Path"] = "/api/unprotected/{**catch-all}",
        };
}
