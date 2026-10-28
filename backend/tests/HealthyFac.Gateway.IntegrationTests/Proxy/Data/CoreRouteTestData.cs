namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class CoreRouteTestData
{
    public static TheoryData<string> UnversionedPaths => new()
    {
        "/api/core/hc",
        "/api/core/health/ready",
        "/api/core/openapi/v1.json",
    };
}
