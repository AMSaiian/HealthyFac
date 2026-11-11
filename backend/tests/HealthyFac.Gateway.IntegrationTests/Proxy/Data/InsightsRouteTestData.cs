namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class InsightsRouteTestData
{
    public const string VersionedPath = "/api/insights/v1/files/abc/ingest-attempts";
    public const string KeptHeader = "X-Test-Kept";
    public const string KeptValue = "kept";
    public const string ForgedActorId = "0190f3a0-0000-7000-8000-0000000000f1";
    public const string ForgedCompanyId = "0190f3a0-0000-7000-8000-0000000000f2";

    public static TheoryData<string> UnversionedPaths => new()
    {
        "/api/insights/hc",
        "/api/insights/health/ready",
        "/api/insights/openapi/v1.json",
    };
}
