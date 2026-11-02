namespace HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

public static class ScopedServiceTokenTestData
{
    public const string ServiceClientId = "healthyfac-test-service";
    public const string ServiceClientSecret = "test-service-client-secret";
    public const string ServiceRoleName = "test-service";
    public const string FirstPermission = "test:first";
    public const string SecondPermission = "test:second";
    public const string FirstScope = "test-service-first";
    public const string SecondScope = "test-service-second";
    public const string BothScopes = "test-service-first test-service-second";
}
