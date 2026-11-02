namespace HealthyFac.Gateway.IntegrationTests.CrossOrigin.Data;

public static class GatewayCorsTestData
{
    public const string CoreInvitesRoute = "/api/core/v1/employee-invites";
    public const string VerifyInviteRoute = "/api/core/v1/employee-invites/verify";
    public const string MachinesRoute = "/api/core/v1/machines";
    public const string TokenRoute = "/realms/healthyfac/protocol/openid-connect/token";
    public const string AllowOriginHeader = "Access-Control-Allow-Origin";
    public const string RequestHeaders = "authorization,content-type";
    public const int PreflightCount = 5;

    public static readonly HttpMethod PreflightMethod = HttpMethod.Options;

    private const string KeycloakAddressKey = "ReverseProxy:Clusters:keycloak:Destinations:main:Address";

    public static IReadOnlyDictionary<string, string> CreateKeycloakSettings(string keycloakAddress) =>
        new Dictionary<string, string> { [KeycloakAddressKey] = keycloakAddress };
}
