namespace HealthyFac.Shared.Web.Versioning;

public static class ApiVersioningConstants
{
    public const int DefaultMajorVersion = 1;

    public const string RoutePrefix = "/v{version:apiVersion}";
}
