using System.Net;
using System.Text;

namespace HealthyFac.Shared.UnitTests.HttpFailures.Data;

public static class HttpResponseFactory
{
    public static HttpResponseMessage Create(
        HttpStatusCode statusCode,
        string url,
        string? body = null,
        HttpContent? content = null
    ) =>
        new(statusCode)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Post, url),
            Content = content ?? new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json"),
        };
}
