using System.Net;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Defines the exceptions of the service token request. The texts never hold the client secret or a token.
/// </summary>
public static class ServiceTokenErrors
{
    public static HttpRequestException TokenRequestFailed(HttpStatusCode statusCode) =>
        new($"The token endpoint answered {(int)statusCode} to the Client Credentials request.", null, statusCode);

    public static InvalidOperationException TokenResponseInvalid() =>
        new("The token endpoint answered success, but the response has no access token or no valid lifetime.");
}
