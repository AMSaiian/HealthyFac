using System.Net;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

/// <summary>
/// Plays the target service. It answers success and keeps the bearer token of the request.
/// </summary>
public sealed class EchoHandler : HttpMessageHandler
{
    public string? ReceivedToken { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ReceivedToken = request.Headers.Authorization?.Parameter;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
