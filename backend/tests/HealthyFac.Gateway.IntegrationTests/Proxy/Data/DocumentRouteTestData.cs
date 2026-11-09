namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class DocumentRouteTestData
{
    public const long BodyAboveCoreLimitBytes = 40L * 1024 * 1024;
    public const long BodyAboveRouteLimitBytes = 102L * 1024 * 1024;
    public const string UploadPath = "/api/document/v1/files";
    public const string CoreUploadPath = "/api/core/v1/machines";

    public static TheoryData<string> UnversionedPaths => new()
    {
        "/api/document/hc",
        "/api/document/health/ready",
        "/api/document/openapi/v1.json",
    };
}
