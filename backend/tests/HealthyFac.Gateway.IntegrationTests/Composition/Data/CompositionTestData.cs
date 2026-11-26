namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

public static class CompositionTestData
{
    public const string Path = "/v1/drafts/probe";
    public const string BearerToken = "Bearer test-token-value";
    public const string ActorIdHeader = "healthyfac-actor-id";
    public const string CompanyIdHeader = "healthyfac-company-id";
    public const string TraceParentHeader = "traceparent";
    public const string OkJson = "{\"items\":[1,2]}";
    public const string ProblemJson = "{\"title\":\"Not found\",\"status\":404}";
    public const string GatewaySettingsFile = "backend/src/Gateway/HealthyFac.Gateway.Web/appsettings.json";
    public const string GatewayComposeConfigFile = "docker/config/gateway.json";

    public static readonly string[] ComposedClusterIds = ["core", "telemetry"];

    public static TheoryData<string> ConfigurationFiles => [GatewaySettingsFile, GatewayComposeConfigFile];
}
