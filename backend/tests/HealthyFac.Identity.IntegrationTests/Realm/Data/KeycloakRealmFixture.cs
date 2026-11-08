using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public sealed class KeycloakRealmFixture : IAsyncLifetime
{
    public KeycloakTestServer Server { get; } = new();

    public async ValueTask InitializeAsync() => await Server.StartAsync(RealmTestData.Environment);

    public async ValueTask DisposeAsync() => await Server.DisposeAsync();
}
