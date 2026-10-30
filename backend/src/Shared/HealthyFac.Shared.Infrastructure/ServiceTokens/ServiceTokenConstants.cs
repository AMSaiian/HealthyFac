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

    public static class Form
    {
        public const string GrantType = "grant_type";
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string ClientCredentialsGrant = "client_credentials";
    }

    public static class Response
    {
        public const string AccessToken = "access_token";
        public const string ExpiresIn = "expires_in";
    }
}
