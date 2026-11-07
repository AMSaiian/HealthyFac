namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class TelemetryRouteTestData
{
    public static TheoryData<string> UnversionedPaths => new()
    {
        "/api/telemetry/hc",
        "/api/telemetry/health/ready",
        "/api/telemetry/openapi/v1.json",
    };
}
