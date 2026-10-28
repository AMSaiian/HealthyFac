namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class RouteHostTestData
{
    public const string ApiHost = "api.healthyfac.test";
    public const string AuthHost = "auth.healthyfac.test";
    public const string CoreUrl = $"http://{ApiHost}/api/core/v1/machines";
    public const string CoreUrlOnAuthHost = $"http://{AuthHost}/api/core/v1/machines";
    public const string KeycloakPath = "/realms/healthyfac/.well-known/openid-configuration";
    public const string KeycloakUrl = $"http://{AuthHost}{KeycloakPath}";
    public const string KeycloakUrlOnApiHost = $"http://{ApiHost}{KeycloakPath}";

    private const string KeycloakAddressKey = "ReverseProxy:Clusters:keycloak:Destinations:main:Address";

    private static readonly string[] _apiRoutes = ["core", "core-invite-public"];
    private static readonly string[] _authRoutes = ["keycloak-token", "keycloak-realm", "keycloak-resources"];

    public static IReadOnlyDictionary<string, string> CreateSettings(string keycloakAddress)
    {
        var settings = new Dictionary<string, string> { [KeycloakAddressKey] = keycloakAddress };

        foreach (var route in _apiRoutes)
        {
            settings[$"ReverseProxy:Routes:{route}:Match:Hosts:0"] = ApiHost;
        }

        foreach (var route in _authRoutes)
        {
            settings[$"ReverseProxy:Routes:{route}:Match:Hosts:0"] = AuthHost;
        }

        return settings;
    }
}
