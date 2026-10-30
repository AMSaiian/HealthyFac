using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Wires a client through <see cref="ClientCredentialsHandler"/> to a fake API, with a fake token endpoint and a fake
/// clock.
/// </summary>
public sealed class ServiceTokenHarness : IDisposable
{
    public ServiceTokenHarness()
    {
        var cache = new ServiceTokenCache(
            new FixedHttpClientFactory(TokenEndpoint),
            Options.Create(ServiceTokenTestData.Options),
            Time);

        Client = new HttpClient(new ClientCredentialsHandler(cache) { InnerHandler = Resource });
    }

    public FakeTokenEndpoint TokenEndpoint { get; } = new();

    public RecordingResourceHandler Resource { get; } = new();

    public FakeTimeProvider Time { get; } = new();

    public HttpClient Client { get; }

    public Task<HttpResponseMessage> CallAsync() => Client.GetAsync(ServiceTokenTestData.ResourceUrl);

    /// <summary>
    /// Starts calls while the token endpoint holds its answer, then waits until every call has reached the cache.
    /// </summary>
    /// <param name="count">The number of parallel calls.</param>
    /// <returns>The pending calls. Release the token endpoint to complete them.</returns>
    public async Task<IReadOnlyList<Task<HttpResponseMessage>>> StartParallelCallsAsync(int count)
    {
        TokenEndpoint.HoldResponses();
        using var allStarted = new CountdownEvent(count);

        var calls = Enumerable.Range(0, count)
            .Select(_ => Task.Run(() =>
            {
                allStarted.Signal();

                return CallAsync();
            }))
            .ToList();

        await Task.Run(allStarted.Wait);
        await Task.Delay(ServiceTokenTestData.CallsReachCacheDelay);

        return calls;
    }

    public void Dispose() => Client.Dispose();

    private sealed class FixedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
