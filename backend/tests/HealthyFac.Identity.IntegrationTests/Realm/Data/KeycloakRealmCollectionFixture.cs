using HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;
using HealthyFac.Identity.IntegrationTests.TokenExchange.Data;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Resetting;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// Starts the realm once and adds the temporary clients, roles, and scopes of the scoped service token tests and of the
/// token exchange tests to it. It also starts Redis, because the token store of <c>AddServiceTokens</c> keeps the
/// exchanged tokens there. The reset deletes the users and the clients that the tests created, and empties Redis.
/// </summary>
public sealed class KeycloakRealmCollectionFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly RedisTestServer _redis = new();

    public KeycloakRealmCollectionFixture()
    {
        ScopedServiceToken = new ScopedServiceTokenFixture(Realm);
        TokenExchange = new TokenExchangeFixture(Realm, _redis);
    }

    public KeycloakRealmFixture Realm { get; } = new();

    public ScopedServiceTokenFixture ScopedServiceToken { get; }

    public TokenExchangeFixture TokenExchange { get; }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(Realm.InitializeAsync().AsTask(), _redis.StartAsync());

        await ScopedServiceToken.InitializeAsync();
        await TokenExchange.InitializeAsync();
    }

    public async ValueTask ResetAsync()
    {
        await Realm.Server.DeleteTestIdentitiesAsync();
        await _redis.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Realm.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
