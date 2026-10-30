using System.Collections.Concurrent;
using System.Net;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays the API that the handler protects. It records the <c>Authorization</c> header of each request that
/// reaches it.
/// </summary>
public sealed class RecordingResourceHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<string?> _authorizations = new();

    public IReadOnlyCollection<string?> Authorizations => _authorizations;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _authorizations.Enqueue(request.Headers.Authorization?.ToString());

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
