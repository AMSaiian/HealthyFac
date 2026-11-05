using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Exchange;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Wires a client through <see cref="TokenExchangeHandler"/> to a fake API, with a fake token endpoint, a fake
/// incoming request, a fake clock, and an in-memory token storage.
/// </summary>
public sealed class TokenExchangeHarness : IDisposable
{
    public TokenExchangeHarness()
    {
        var serviceOptions = Options.Create(new ServiceOptions { Name = ServiceTokenTestData.ServiceName });
        var store = new TokenStore(
            Storage,
            Time,
            Options.Create(ServiceTokenTestData.Options),
            serviceOptions,
            NullLogger<TokenStore>.Instance);
        var exchangeClient = new TokenExchangeClient(
            new TokenEndpointClient(
                new FixedHttpClientFactory(TokenEndpoint),
                Options.Create(ServiceTokenTestData.Options),
                Time,
                new FixedRandom(0)),
            store,
            serviceOptions);

        Client = new HttpClient(
            new TokenExchangeHandler(
                Incoming,
                exchangeClient,
                ServiceTokenTestData.TargetAudience,
                ServiceTokenTestData.TargetScope) { InnerHandler = Resource });
    }

    public FakeTokenEndpoint TokenEndpoint { get; } = new();

    public InMemoryTokenCacheStorage Storage { get; } = new();

    public RecordingResourceHandler Resource { get; } = new();

    public FakeIncomingAccessTokenAccessor Incoming { get; } = new()
    {
        AccessToken = ServiceTokenTestData.FirstSubjectToken,
        UserId = ServiceTokenTestData.FirstUserId,
    };

    public FakeTimeProvider Time { get; } = new();

    public HttpClient Client { get; }

    public Task<HttpResponseMessage> CallAsync() => Client.GetAsync(ServiceTokenTestData.ResourceUrl);

    /// <summary>
    /// Switches the incoming request to another user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="accessToken">The access token of the user.</param>
    public void SignInAs(Guid userId, string accessToken)
    {
        Incoming.UserId = userId;
        Incoming.AccessToken = accessToken;
    }

    public void Dispose() => Client.Dispose();
}
