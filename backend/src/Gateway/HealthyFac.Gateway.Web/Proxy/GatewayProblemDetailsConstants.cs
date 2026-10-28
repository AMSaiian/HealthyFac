namespace HealthyFac.Gateway.Web.Proxy;

/// <summary>
/// Defines the titles, type URLs, and details of the problem details that the gateway writes itself, grouped by
/// the error. The services use the same <c>application/problem+json</c> format.
/// </summary>
public static class GatewayProblemDetailsConstants
{
    public static class TooManyRequests
    {
        public const string Title = "Too Many Requests";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc6585#section-4";
        public const string Detail = "The request limit is reached. Retry after the time in the Retry-After header.";
    }

    public static class Unauthorized
    {
        public const string Title = "Unauthorized";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7235#section-3.1";
        public const string Detail = "The access token is not valid. Sign in again.";
    }

    public static class BadGateway
    {
        public const string Title = "Bad Gateway";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.6.3";
        public const string Detail = "The service did not answer correctly. Retry later.";
    }

    public static class ServiceUnavailable
    {
        public const string Title = "Service Unavailable";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.6.4";
        public const string Detail = "No healthy instance of the service is available. Retry later.";
    }

    public static class GatewayTimeout
    {
        public const string Title = "Gateway Timeout";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.6.5";
        public const string Detail = "The service did not answer in time. Retry later.";
    }

    public static class InternalServerError
    {
        public const string Title = "Unhandled exception occurred";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        public const string Detail = "An unexpected error occurred. Contact support if it persists.";
    }
}
