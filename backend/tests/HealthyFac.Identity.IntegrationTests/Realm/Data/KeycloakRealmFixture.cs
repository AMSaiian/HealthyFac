using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public sealed class KeycloakRealmFixture : IAsyncLifetime
{
    public KeycloakTestServer Server { get; } = new();

    public Task InitializeAsync() => Server.StartAsync(RealmTestData.Environment);

    public async Task DisposeAsync() => await Server.DisposeAsync();
}
