namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

public static class TokenExchangeTestData
{
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";
    public const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";
    public const string TargetClientId = "healthyfac-test-target";
    public const string TargetRoleName = "test-target:read";
    public const string TargetScopeName = "test-target";
    public const string ExchangeScopes = "test-target organization healthyfac-claims";
    public const string NotAllowedAudience = "healthyfac-spa";
    public const string OrganizationClaim = "organization";
    public const string IssuerClaim = "iss";
    public const string SubjectClaim = "sub";
    public const string TokenPath = "realms/healthyfac/protocol/openid-connect/token";
    public const string EchoClientName = "echo";
    public const string EchoUrl = "https://target.example.test/echo";
    public const string ErrorField = "error";
    public const string InvalidTokenError = "invalid_request";
}
