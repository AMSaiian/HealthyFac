using System.Collections.Concurrent;
using System.Net;
using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays the API that the handler protects. It records the <c>Authorization</c> header and the actor headers of
/// each request that reaches it.
/// </summary>
public sealed class RecordingResourceHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<string?> _authorizations = new();
    private readonly ConcurrentQueue<(string? ActorId, string? CompanyId)> _actorHeaders = new();

    public IReadOnlyCollection<string?> Authorizations => _authorizations;

    public IReadOnlyCollection<(string? ActorId, string? CompanyId)> ActorHeaders => _actorHeaders;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _authorizations.Enqueue(request.Headers.Authorization?.ToString());
        _actorHeaders.Enqueue(
            (ReadHeader(request, ActorHeaderNames.ActorId), ReadHeader(request, ActorHeaderNames.CompanyId)));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private static string? ReadHeader(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
