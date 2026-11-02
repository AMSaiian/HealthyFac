using System.Net;

namespace HealthyFac.Shared.Infrastructure.HttpFailures;

/// <summary>
/// Defines the exception of a failed outgoing HTTP call. The text never holds a query string, a request body, or a
/// raw response body.
/// </summary>
public static class HttpFailureErrors
{
    private const char QuerySeparator = '?';
    private const char FragmentSeparator = '#';

    public static HttpRequestException RequestFailed(
        HttpMethod? method,
        Uri? uri,
        HttpStatusCode statusCode,
        string? reason
    )
    {
        var call = method is null || uri is null ? "The HTTP call" : $"The call {method} {WithoutQuery(uri)}";
        var message = $"{call} failed with {(int)statusCode} {statusCode}.";

        return new HttpRequestException(
            reason is null
                ? message
                : $"{message} Reason: {reason}", null, statusCode);
    }

    private static string WithoutQuery(Uri uri) =>
        uri.IsAbsoluteUri
            ? uri.GetLeftPart(UriPartial.Path)
            : uri.OriginalString.Split(QuerySeparator, FragmentSeparator)[0];
}
