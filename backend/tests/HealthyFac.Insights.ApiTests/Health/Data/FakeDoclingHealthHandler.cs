using System.Net;

namespace HealthyFac.Insights.ApiTests.Health.Data;

/// <summary>
/// Plays the health path of docling-serve: it answers with a status, throws a network error, or never answers.
/// </summary>
public sealed class FakeDoclingHealthHandler : HttpMessageHandler
{
    public HttpStatusCode? Status { get; init; }

    public bool IsHanging { get; init; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (IsHanging)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return Status is { } status
            ? new HttpResponseMessage(status)
            : throw new HttpRequestException("Docling is down.");
    }
}
