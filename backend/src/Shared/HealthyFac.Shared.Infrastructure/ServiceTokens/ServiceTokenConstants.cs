namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Defines the names and limits of the Client Credentials token request.
/// </summary>
public static class ServiceTokenConstants
{
    /// <summary>The name of the HTTP client that calls the token endpoint. It carries no token handler.</summary>
    public const string TokenClientName = "service-token";

    /// <summary>The scheme of the <c>Authorization</c> header that the handler sets.</summary>
    public const string BearerScheme = "Bearer";

    /// <summary>The scope that makes Keycloak write the company of the user into an exchanged token.</summary>
    public const string OrganizationScope = "organization";

    /// <summary>The separator of the scope names in a <c>scope</c> form field.</summary>
    public const char ScopeSeparator = ' ';

    /// <summary>
    /// The separator between the service name and the name of a downstream scope in a Keycloak scope.
    /// </summary>
    public const char DownstreamScopeSeparator = '-';

    public static class Form
    {
        public const string GrantType = "grant_type";
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string ClientCredentialsGrant = "client_credentials";
        public const string SubjectToken = "subject_token";
        public const string SubjectTokenType = "subject_token_type";
        public const string Audience = "audience";
        public const string Scope = "scope";
        public const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
        public const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";
    }

    public static class Response
    {
        public const string AccessToken = "access_token";
        public const string ExpiresIn = "expires_in";
    }
}
