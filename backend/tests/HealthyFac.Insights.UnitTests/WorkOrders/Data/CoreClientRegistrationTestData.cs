namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

public static class CoreClientRegistrationTestData
{
    public const string ServiceToken = "insights-service-token";
    public const string GrpcAddress = "http://core.test:8081";
    public const string TokenEndpoint = "https://auth.test/token";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000d01");

    public static readonly IReadOnlyDictionary<string, string?> Settings = new Dictionary<string, string?>
    {
        ["Service:Name"] = "insights",
        ["CoreClient:GrpcAddress"] = GrpcAddress,
        ["ServiceTokens:TokenEndpoint"] = TokenEndpoint,
        ["ServiceTokens:ClientId"] = "healthyfac-insights",
        ["ServiceTokens:ClientSecret"] = "test-secret",
    };
}
