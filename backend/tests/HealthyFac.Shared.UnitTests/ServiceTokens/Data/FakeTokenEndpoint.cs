using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays the token endpoint. It answers <c>token-N</c> to the Nth request and records every form body.
/// </summary>
public sealed class FakeTokenEndpoint : HttpMessageHandler
{
    private readonly ConcurrentQueue<string> _forms = new();
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _requestCount;
    private bool _isGateEnabled;

    public int RequestCount => Volatile.Read(ref _requestCount);

    public IReadOnlyCollection<string> Forms => _forms;

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public int ExpiresInSeconds { get; set; } = ServiceTokenTestData.TokenLifetimeSeconds;

    public void HoldResponses() => _isGateEnabled = true;

    public void ReleaseResponses() => _gate.SetResult();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var number = Interlocked.Increment(ref _requestCount);
        var form = await request.Content!.ReadAsStringAsync(cancellationToken);
        _forms.Enqueue(form);

        if (_isGateEnabled)
        {
            await _gate.Task.WaitAsync(cancellationToken);
        }

        if (StatusCode != HttpStatusCode.OK)
        {
            return new HttpResponseMessage(StatusCode);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { access_token = $"token-{number}", expires_in = ExpiresInSeconds }),
        };
    }
}
