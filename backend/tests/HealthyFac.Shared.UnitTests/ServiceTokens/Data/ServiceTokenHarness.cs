using System.Web;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Wires a client through <see cref="ClientCredentialsHandler"/> to a fake API, with a fake token endpoint, a fake
/// clock, and an in-memory token storage. <see cref="CreateClient"/> adds a client of another instance of the
/// service. All instances share the storage, the token endpoint, and the clock, like instances that share Redis.
/// </summary>
public sealed class ServiceTokenHarness : IDisposable
{
    private readonly double _jitterFraction;
    private readonly List<HttpClient> _clients = [];

    /// <summary>
    /// Creates the harness.
    /// </summary>
    /// <param name="jitterFraction">The fraction of the maximum refresh jitter that each token gets, from 0 to 1.
    /// The default 0 gives no jitter, so the token is replaced exactly at the refresh margin.</param>
    public ServiceTokenHarness(double jitterFraction = 0)
    {
        _jitterFraction = jitterFraction;
        Client = CreateClient();
    }

    public FakeTokenEndpoint TokenEndpoint { get; } = new();

    public InMemoryTokenCacheStorage Storage { get; } = new();

    public DownstreamScopeHolder DownstreamScopes { get; } = new();

    public RecordingResourceHandler Resource { get; } = new();

    public TimerSignalingTimeProvider Time { get; } = new();

    public HttpClient Client { get; }

    public Task<HttpResponseMessage> CallAsync() => Client.GetAsync(ServiceTokenTestData.ResourceUrl);

    /// <summary>
    /// Creates the client of one more instance of the service. It has its own cache and its own token store.
    /// </summary>
    /// <returns>The client. The harness disposes it.</returns>
    public HttpClient CreateClient()
    {
        var serviceOptions = Options.Create(new ServiceOptions { Name = ServiceTokenTestData.ServiceName });
        var store = new TokenStore(
            Storage,
            Time,
            Options.Create(ServiceTokenTestData.Options),
            serviceOptions,
            NullLogger<TokenStore>.Instance);
        var cache = new ServiceTokenCache(
            new TokenEndpointClient(
                new FixedHttpClientFactory(TokenEndpoint),
                Options.Create(ServiceTokenTestData.Options),
                Time,
                new FixedRandom(_jitterFraction)),
            store,
            serviceOptions);
        var client = new HttpClient(
            new ClientCredentialsHandler(cache, DownstreamScopes, serviceOptions) { InnerHandler = Resource });
        _clients.Add(client);

        return client;
    }

    /// <summary>
    /// Starts calls while the token endpoint holds its answer, then waits until every call has reached the cache. One
    /// call holds the storage lock and waits for the token endpoint. The others wait on the fake clock and read the
    /// storage again when the test moves the clock by <see cref="ServiceTokenTestData.PollAdvance"/>.
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

    /// <summary>
    /// Reads the <c>scope</c> field of the only token request.
    /// </summary>
    /// <returns>The scopes separated by spaces, or <see langword="null"/> when the request has no scope.</returns>
    public string? ReadScopes() =>
        HttpUtility.ParseQueryString(TokenEndpoint.Forms.Single())[ServiceTokenConstants.Form.Scope];

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
    }
}
